using Castle.Core;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.DocumentStore.Shared.Jobs;
using Jarvis.Framework.Shared.MultitenantSupport;
using Jarvis.Framework.Shared.ReadModel;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
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

namespace Jarvis.DocumentStore.Core.Jobs.QueueManager
{
    public interface IQueueManager
    {
        QueuedJob GetNextJob(String queueName, String identity, String callerHandle, TenantId tenant, Dictionary<String, Object> customData);

        Boolean SetJobExecuted(String queueName, String jobId, String errorMessage);

        /// <summary>
        /// Internally used to grab a job reference from its id.
        /// </summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="jobId"></param>
        /// <returns></returns>
        QueuedJob GetJob(String queueName, String jobId);

        /// <summary>
        /// Given an handle, ask to queue manager to schedule
        /// again all jobs as if the handle was just inserted
        /// into the system.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="tenant"></param>
        /// <returns></returns>
        Boolean ReQueueJobs(DocumentHandle handle, TenantId tenant);

        Boolean ReScheduleFailed(String queueName);

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

        private MongoCollection<StreamCheckpoint> _checkpointCollection;

        private QueueTenantInfo[] _queueTenantInfos;

        private Dictionary<String, QueueHandler> _queueHandlers;

        public ILogger Logger { get; set; }

        public QueueManager(
            MongoDatabase mongoDatabase,
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
                        Query<StreamCheckpoint>.EQ(t => t.TenantId, tenantId)
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

        public Boolean ReQueueJobs(DocumentHandle handle, TenantId tenant)
        {
            //Basically, rescheduling a job just means re-create all the jobs
            //as if the "original" format was added to an handle.
            var info = _queueTenantInfos.SingleOrDefault(i => i.TenantId == tenant);
            if (info == null)
                throw new ArgumentException("Invalid tenant " + tenant, "tenant");
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
                qh.Value.Handle(lastStream, info.TenantId, forceReSchedule : true);
            }

            return true;
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
                                if (streamData.EventType != HandleStreamEventTypes.DocumentHasNewFormat &&
                                    streamData.EventType != HandleStreamEventTypes.DocumentFormatUpdated) continue;

                                qh.Value.Handle(streamData, info.TenantId);
                            }
                        }
                        info.Checkpoint = blockOfStreamData[blockOfStreamData.Count - 1].Id;
                        _checkpointCollection.Save(new StreamCheckpoint() { Checkpoint = info.Checkpoint, TenantId = info.TenantId });
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
            if (!_isRebuilding)  PollNow();
        }

        public QueuedJob GetNextJob(String queueName, String identity, String handle, TenantId tenant, Dictionary<String, Object> customData)
        {
            return ExecuteWithQueueHandler("get next job", queueName, qh => qh.GetNextJob(identity, handle, tenant, customData)) as QueuedJob;
        }

        public QueuedJob GetJob(String queueName, string jobId)
        {
            return ExecuteWithQueueHandler("get job", queueName, qh => qh.GetJob(jobId)) as QueuedJob;
        }

        public Boolean SetJobExecuted(String queueName, String jobId, String errorMessage)
        {
            return (Boolean)ExecuteWithQueueHandler("set job executed", queueName, qh => qh.SetJobExecuted(jobId, errorMessage));
        }

        public Boolean ReScheduleFailed(String queueName)
        {
            return ExecuteWithQueueHandler("reschedule failed jobs", queueName, qh => qh.ReScheduleFailed());

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
