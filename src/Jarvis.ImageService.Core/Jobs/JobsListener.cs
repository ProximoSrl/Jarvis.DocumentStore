using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Quartz;

namespace Jarvis.ImageService.Core.Jobs
{
    public class JobsListener : IJobListener
    {
        public JobsListener(ILogger logger)
        {
            Logger = logger;
        }

        private ILogger Logger { get; set; }

        public void JobToBeExecuted(IJobExecutionContext context)
        {
            Logger.DebugFormat("JobToBeExecuted");
        }

        public void JobExecutionVetoed(IJobExecutionContext context)
        {
            Logger.DebugFormat("JobExecutionVetoed");
        }

        public void JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException)
        {
            Logger.DebugFormat("JobWasExecuted");
            if (jobException != null)
            {
                Logger.DebugFormat("With exception... rescheduling");
            }
        }

        public string Name {
            get { return "pipeline.listener"; }
        }
    }
}
