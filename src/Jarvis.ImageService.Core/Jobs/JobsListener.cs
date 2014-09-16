using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Newtonsoft.Json;
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
            if (jobException != null)
            {
                jobException.UnscheduleAllTriggers = true;

                var ex = jobException.GetBaseException();
                var retries = context.Trigger.JobDataMap.GetIntValue("_retrycount") +1;
                Logger.ErrorFormat(ex, "Refire count {0}", retries);

                try
                {
                    if (retries < 5)
                    {
                        var rescheduleAt = DateTime.Now.AddSeconds(5*retries);
                        Logger.DebugFormat("Rescheduling job {0} at {1}", context.JobDetail.Key, rescheduleAt);
                        context.Scheduler.RescheduleJob(
                            context.Trigger.Key,
                            TriggerBuilder
                                .Create()
                                .UsingJobData("_retrycount", retries)
                                .StartAt(rescheduleAt)
                            .Build()
                        );
                    }
                    else
                    {
                        Logger.ErrorFormat("Too many errors on job {0} with data {1}", 
                            context.JobDetail.JobType,
                            JsonConvert.SerializeObject(context.JobDetail.JobDataMap)
                        );
                    }
                }
                catch (Exception rescheduleException)
                {
                    Logger.Fatal("rescheduling", rescheduleException);
                }
            }
        }

        public string Name
        {
            get { return "pipeline.listener"; }
        }
    }
}
