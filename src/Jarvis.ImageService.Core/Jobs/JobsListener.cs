using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.ProcessingPipeline;
using Jarvis.ImageService.Core.Services;
using Jarvis.ImageService.Core.Storage;
using Newtonsoft.Json;
using Quartz;

namespace Jarvis.ImageService.Core.Jobs
{
    public class JobsListener : IJobListener
    {
        readonly IConversionWorkflow _conversionWorkflow;
        readonly ILogger _logger;

        public JobsListener(ILogger logger, IConversionWorkflow conversionWorkflow)
        {
            _conversionWorkflow = conversionWorkflow;
            _logger = logger;
        }


        public void JobToBeExecuted(IJobExecutionContext context)
        {
        }

        public void JobExecutionVetoed(IJobExecutionContext context)
        {
        }

        public void JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException)
        {
            if (typeof (AbstractFileJob).IsAssignableFrom(context.JobDetail.JobType))
            {
                _logger.DebugFormat("Handling job post-execution {0}", context.JobDetail.JobType);
                HandleFileJob(context, jobException);
            }
            else
            {
                _logger.DebugFormat("Ignored job post-execution {0}", context.JobDetail.JobType);
            }
        }

        void HandleFileJob(IJobExecutionContext context, JobExecutionException jobException)
        {
            var fileId = new FileId(context.JobDetail.JobDataMap.GetString(JobKeys.FileId));

            if (jobException != null)
            {
                HandleErrors(context, jobException);
            }
            else
            {
                var nextJob = context.JobDetail.JobDataMap.GetString(JobKeys.NextJob);
                if (nextJob == null)
                    return;

                _conversionWorkflow.Next(fileId, nextJob);
            }
        }

        void HandleErrors(IJobExecutionContext context, JobExecutionException jobException)
        {
            jobException.UnscheduleAllTriggers = true;

            var ex = jobException.GetBaseException();
            var retries = context.Trigger.JobDataMap.GetIntValue("_retrycount") + 1;
            _logger.ErrorFormat(ex, "Refire count {0}", retries);

            try
            {
                if (retries < 5)
                {
                    var rescheduleAt = DateTime.Now.AddSeconds(5 * retries);
                    _logger.DebugFormat("Rescheduling job {0} at {1}", context.JobDetail.Key, rescheduleAt);
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
                    _logger.ErrorFormat("Too many errors on job {0} with data {1}",
                        context.JobDetail.JobType,
                        JsonConvert.SerializeObject(context.JobDetail.JobDataMap)
                        );
                }
            }
            catch (Exception rescheduleException)
            {
                _logger.Fatal("rescheduling", rescheduleException);
            }
        }

        public string Name
        {
            get { return "pipeline.listener"; }
        }
    }
}
