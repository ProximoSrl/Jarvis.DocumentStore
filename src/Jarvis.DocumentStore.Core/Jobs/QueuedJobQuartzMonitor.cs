using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using CQRS.Kernel.ProjectionEngine.RecycleBin;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Shared.Jobs;
using NEventStore;
using Quartz;
using Jarvis.DocumentStore.Core.Jobs.PollingJobs;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Jarvis.DocumentStore.Core.Support;

namespace Jarvis.DocumentStore.Core.Jobs
{
    /// <summary>
    /// This jobs monitors <see cref="QueuedJob" /> collection to verify is some job is blocked
    /// and there is the need to restart the worker.
    /// </summary>
    [DisallowConcurrentExecution]
    public class QueuedJobQuartzMonitor : ISystemJob
    {
        readonly IPollerJobManager _pollerJobManager;
        readonly QueueHandler[] _queueHandlers;
        readonly DocumentStoreConfiguration _config;
        public ILogger Logger { get; set; }

        public QueuedJobQuartzMonitor(
            IPollerJobManager pollerJobManager,
            QueueHandler[] queueHandlers,
            DocumentStoreConfiguration config)
        {
            _pollerJobManager = pollerJobManager;
            _queueHandlers = queueHandlers;
            _config = config;
            new List<String>() 
            {
                config.ServerAddress.AbsoluteUri
            };
            Logger = NullLogger.Instance;
        }

        public void Execute(IJobExecutionContext context)
        {
            foreach (var qh in _queueHandlers)
            {
                var blockedJobs = qh.GetBlockedJobs();
                Logger.DebugFormat("Queue {0} has {1} blocked jobs!", qh.Name, blockedJobs.Count);
                //if we have blocked jobs, probably the worker is blocked.
                var allBlockedHandles = blockedJobs
                    .Select(j => j.ExecutingHandle)
                    .Distinct();
                foreach (var blockedHandle in allBlockedHandles)
	            {
                    Logger.WarnFormat("Unblock jobs for queue {0} and handle {1}", qh.Name, blockedHandle);
                    _pollerJobManager.Restart(blockedHandle);
	            }
                foreach (var job in blockedJobs)
                {
                    Logger.WarnFormat("Unblock job {0}", job.Id);
                    
                    qh.SetJobExecuted(job.Id, "Timeout, unblocked by QueuedJobQuartzMonitor");
                }
            }
        }
    }
}
