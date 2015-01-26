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
using NEventStore;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    /// <summary>
    /// This jobs monitors <see cref="QueuedJob" /> collection to verify is some job is blocked.
    /// </summary>
    [DisallowConcurrentExecution]
    public class QueuedJobQuartzMonitor : ISystemJob
    {
        
        public void Execute(IJobExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
