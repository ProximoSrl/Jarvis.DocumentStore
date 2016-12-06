using Castle.Core;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.DocumentStore.Shared.Jobs;
using Jarvis.Framework.Shared.MultitenantSupport;
using Jarvis.Framework.Shared.ReadModel;
using MongoDB.Driver;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Shared.Model;
using Jarvis.Framework.Kernel.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.Framework.Shared.Helpers;

namespace Jarvis.DocumentStore.Core.Jobs.QueueManager
{
    /// <summary>
    /// This is the generic interface for a manager of jobs queue, its purpose
    /// is to give access to jobs to external clients, generate jobs monitoring
    /// the Stream readmodel.
    /// 
    /// Its duty is to manage all the single instance of the queues, generating
    /// jobs for the executor and being an arbiter for the real object that physically
    /// manage queues, the <see cref="QueueHandler"/> component.
    /// </summary>
    public interface IQueueManager
    {
        QueuedJob GetNextJob(TenantId tenantId, String queueName, String identity, String callerHandle, Dictionary<String, Object> customData);

        /// <summary>
        /// Set the job as executed
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="jobId"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        Boolean SetJobExecuted(String queueName, String jobId, String errorMessage);

        /// <summary>
        /// This function add a job from external code. Usually jobs are automatically
        /// created after an handle is loaded in document store or after a new 
        /// format is present, but some queue, as PdfComposer, creates job only from
        /// manual input.
        /// </summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="job"></param>
        Boolean QueueJob(String queueName, QueuedJob job);

        /// <summary>
        /// When a job is executing it can ask the system to requeue job because it
        /// is not capable of hanling it right now.
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="jobId"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        Boolean ReQueueJob(String queueName, String jobId, String errorMessage, TimeSpan timestpan);

        /// <summary>
        /// Internally used to grab a job reference from its id.
        /// </summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="jobId"></param>
        /// <returns></returns>
        QueuedJob GetJob(String queueName, String jobId);

        /// <summary>
        /// This method gets reference for all jobs of selected queues, and return 
        /// to the caller all data about thejobs.
        /// </summary>
        /// <param name="tenantId">Tenant</param>
        /// <param name="handle">Handle of the document</param>
        /// <param name="queueNames">This contains the list of the queues the caller is interested
        /// to know the list of jobs. If this value is null or contains no element it means that 
        /// we want to retrieve jobs for ALL the queue.</param>
        /// <returns></returns>
        QueuedJobInfo[] GetJobsForHandle(TenantId tenantId, DocumentHandle handle, IEnumerable<String> queueNames);

        /// <summary>
        /// Given an handle, ask to queue manager to schedule
        /// again all jobs as if the handle was just inserted
        /// into the system.
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="handle"></param>
        /// <returns></returns>
        Boolean ReQueueJobs(TenantId tenantId, DocumentHandle handle);

        Boolean ReScheduleFailed(String queueName);

        /// <summary>
        /// When a job descriptor is deleted because no handle references it
        /// it is time to delete every jobs in the queue.
        /// </summary>
        /// <param name="documentDescriptorId"></param>
        void DeletedJobForDescriptor(DocumentDescriptorId documentDescriptorId);

        void Start();

        void Stop();
    }

    /// <summary>
    /// Creates and maintain all configured queues.
    /// </summary>
    public class QueueManager : IQueueManager, IObserveProjection
    {
        private DocumentStoreConfiguration _configuration;
        private ITenantAccessor _tenantAccessor;
        private Thread _pollerThread;
        private BlockingCollection<CommandData> _commandList;
        private System.Timers.Timer pollerTimer;

        private IMongoCollection<StreamCheckpoint> _checkpointCollection;

        private QueueTenantInfo[] _queueTenantInfos;

        private Dictionary<String, QueueHandler> _queueHandlers;

        public ILogger Logger { get; set; }

        public QueueManager(
            IMongoDatabase mongoDatabase,
            ITenantAccessor tenantAccessor,
            QueueHandler[] queueHandlers,
            DocumentStoreConfiguration configuration)
        {
            _tenantAccessor = tenantAccessor;
            _configuration = configuration;
            _queueHandlers = queueHandlers
                .ToDictionary(qh => qh.Name, qh => qh);
            _checkpointCollection = mongoDatabase.GetCollection<StreamCheckpoint>("stream.checkpoints");
            Logger = NullLogger.Instance;
        }

        private long FindLastCheckpointForTenant(TenantId tenantId)
        {
            var dbCheckpoint = _checkpointCollection.Find(
                        Builders<StreamCheckpoint>.Filter.Eq(t => t.TenantId, tenantId)
                   ).SingleOrDefault();
            return dbCheckpoint != null ? dbCheckpoint.Checkpoint : 0L;
        }

        private class CommandData
        {
            public QueueCommands Command { get; set; }

            internal static CommandData Poll()
            {
                return new CommandData() { Command = QueueCommands.Poll };
            }

            internal static CommandData Exit()
            {
                return new CommandData() { Command = QueueCommands.Exit };
            }
        }

        private enum QueueCommands
        {
            Poll = 0,
            Exit = 1,
        }

        private Boolean _isStarted = false;

        public void Start()
        {
            _queueTenantInfos = _tenantAccessor.Tenants
               .Select(t => new QueueTenantInfo()
               {
                   TenantId = t.Id,
                   Checkpoint = FindLastCheckpointForTenant(t.Id),
                   StreamReader = t.Container.Resolve<IReader<StreamReadModel, Int64>>(),
               })
               .ToArray();
            _commandList = new BlockingCollection<CommandData>();

            _pollerThread = new Thread(PollerFunc);
            _pollerThread.IsBackground = true;
            _pollerThread.Start();

            pollerTimer = new System.Timers.Timer(_configuration.QueueStreamPollInterval);
            pollerTimer.Elapsed += TimerCallback;
            pollerTimer.Start();
            _isStarted = true;
        }

        public void Stop()
        {
            if (!_isStarted) return;
            _isStarted = false;
            _commandList.Add(CommandData.Exit());
            _commandList.CompleteAdding();
            _commandList = null;
        }

        private void PollerFunc(object obj)
        {
            foreach (var command in _commandList.GetConsumingEnumerable())
            {
                try
                {
                    switch (command.Command)
                    {
                        case QueueCommands.Poll:
                            pollerTimer.Stop(); //no need to add more polling if we are already polling
                            Poll();
                            break;

                        case QueueCommands.Exit:
                            return;

                        default:
                            Logger.ErrorFormat("Unknown command {0}", command.Command);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat(ex, "Error in executing command: {0}", command.Command);
                }
                finally
                {
                    if (!pollerTimer.Enabled) pollerTimer.Start();
                }
            }
        }

        public Boolean ReQueueJobs(TenantId tenantId, DocumentHandle handle)
        {
            //Basically, rescheduling a job just means re-create all the jobs
            //as if the "original" format was added to an handle.
            var info = _queueTenantInfos.SingleOrDefault(i => i.TenantId == tenantId);
            if (info == null)
                throw new ArgumentException("Invalid tenant " + tenantId, "tenant");
            var originalFormat = new DocumentFormat("original");
            var lastStream = info.StreamReader.AllUnsorted
                .Where(s => s.FormatInfo.DocumentFormat == originalFormat &&
                        s.Handle == handle)
                .OrderByDescending(s => s.LastModified)
                .FirstOrDefault();

            if (lastStream == null)
                throw new ArgumentException("Invalid handle " + handle + " unable to find stream event for original content of this handle", "handle");

            //Pass the information to all queue handlers.
            foreach (var qh in _queueHandlers)
            {
                qh.Value.Handle(lastStream, info.TenantId, forceReSchedule: true);
            }

            return true;
        }

        public bool ReQueueJob(string queueName, string jobId, string errorMessage, TimeSpan timeSpan)
        {
            return (Boolean)ExecuteWithQueueHandler("set job executed", queueName, qh => qh.ReQueueJob(jobId, errorMessage, timeSpan));
        }

        public bool QueueJob(String queueName, QueuedJob job)
        {
            return (Boolean)ExecuteWithQueueHandler("set job executed", queueName, qh => qh.QueueJob(job));

        }

        private void Poll()
        {
            //do a poll for every tenant.
            Boolean hasNewData;
            do
            {
                hasNewData = false;
                foreach (var info in _queueTenantInfos)
                {
                    var blockOfStreamData = info.StreamReader.AllSortedById
                        .Where(s => s.Id > info.Checkpoint)
                        .Take(50)
                        .ToList();
                    if (blockOfStreamData.Count > 0)
                    {
                        Logger.DebugFormat("Get a block of {0} stream records to process for tenant {1}.", blockOfStreamData.Count, info.TenantId);
                        hasNewData = true;
                        foreach (var streamData in blockOfStreamData)
                        {
                            foreach (var qh in _queueHandlers)
                            {
                                //In this version we are interested only in event for new formats
                                if (streamData.EventType == HandleStreamEventTypes.DocumentHasNewFormat ||
                                    streamData.EventType == HandleStreamEventTypes.DocumentFormatUpdated)
                                {
                                    qh.Value.Handle(streamData, info.TenantId);
                                }
                                else if (streamData.EventType == HandleStreamEventTypes.DocumentDescriptorDeleted)
                                {
                                    qh.Value.DeletedJobForDescriptor(streamData.DocumentDescriptorId);
                                }
                            }
                        }
                        info.Checkpoint = blockOfStreamData[blockOfStreamData.Count - 1].Id;
                        var cp = new StreamCheckpoint() { Checkpoint = info.Checkpoint, TenantId = info.TenantId };
                        _checkpointCollection.Save(cp, cp.TenantId);
                    }
                }
            } while (hasNewData);
        }

        public void PollNow()
        {
            if (!_isStarted) return;
            if (!_commandList.Any(c => c.Command == QueueCommands.Poll))
                _commandList.Add(CommandData.Poll());
        }

        private void TimerCallback(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!_isRebuilding) PollNow();
        }

        public QueuedJob GetNextJob(TenantId tenantId, String queueName, String identity, String handle, Dictionary<String, Object> customData)
        {
            return ExecuteWithQueueHandler("get next job", queueName, qh => qh.GetNextJob(identity, handle, tenantId, customData)) as QueuedJob;
        }

        public QueuedJob GetJob(String queueName, string jobId)
        {
            return ExecuteWithQueueHandler("get job", queueName, qh => qh.GetJob(jobId)) as QueuedJob;
        }

        public QueuedJobInfo[] GetJobsForHandle(TenantId tenantId, DocumentHandle handle, IEnumerable<string> queueNames)
        {
            if (queueNames == null || queueNames.Any() == false)
            {
                //we are interested in all queues
                queueNames = _queueHandlers.Select(h => h.Key).ToArray();
            }

            List<QueuedJobInfo> retValue = new List<QueuedJobInfo>();
            foreach (var queueName in queueNames)
            {
                var jobs = ExecuteWithQueueHandler("get job for handle", queueName, h => h.GetJobsForHandle(tenantId, handle));
                retValue.AddRange(jobs.Select(j => new QueuedJobInfo(
                    j.Id, 
                    queueName, 
                    j.Status == QueuedJobExecutionStatus.Succeeded || j.Status == QueuedJobExecutionStatus.Failed,
                    j.Status == QueuedJobExecutionStatus.Succeeded)));
            }
            return retValue.ToArray();
        }

        public Boolean SetJobExecuted(String queueName, String jobId, String errorMessage)
        {
            return (Boolean)ExecuteWithQueueHandler("set job executed", queueName, qh => qh.SetJobExecuted(jobId, errorMessage));
        }

        public Boolean ReScheduleFailed(String queueName)
        {
            return ExecuteWithQueueHandler("reschedule failed jobs", queueName, qh => qh.ReScheduleFailed());
        }

        public void DeletedJobForDescriptor(DocumentDescriptorId documentDescriptorId)
        {
            foreach (var qh in _queueHandlers)
            {
                qh.Value.DeletedJobForDescriptor(documentDescriptorId);
            }
        }

        private T ExecuteWithQueueHandler<T>(
            String operationName,
            String queueName,
            Func<QueueHandler, T> executor)
        {
            if (_queueHandlers == null || !_queueHandlers.ContainsKey(queueName))
            {
                Logger.ErrorFormat("Requested operation {0} for queue name {1} but no Queue configured with that name", operationName, queueName);
                return default(T);
            }
            var qh = _queueHandlers[queueName];
            return executor(qh);
        }


        private Boolean _isRebuilding = false;

        public void RebuildStarted()
        {
            _isRebuilding = true;
        }

        public void RebuildEnded()
        {
            _isRebuilding = false;
        }

    }

}
