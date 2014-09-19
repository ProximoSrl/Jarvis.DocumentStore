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
        readonly IPipelineScheduler _pipelineScheduler;
        readonly IFileService _fileService;
        readonly ILogger _logger;

        public JobsListener(ILogger logger, IPipelineScheduler pipelineScheduler, IFileService fileService)
        {
            _pipelineScheduler = pipelineScheduler;
            _fileService = fileService;
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
            if (jobException != null)
            {
                HandleErrors(context, jobException);
            }
            else
            {
                var nextJob = context.JobDetail.JobDataMap.GetString(JobKeys.NextJob);
                if (nextJob == null)
                    return;

                var id = new FileId(context.JobDetail.JobDataMap.GetString(JobKeys.FileId));
                var fileInfo = _fileService.GetById(id);

                switch (nextJob)
                {
                    case "thumbnail": _pipelineScheduler.QueueThumbnail(fileInfo);
                        break;

                    case "resize" : _pipelineScheduler.QueueResize(fileInfo);
                        break;

                    default:
                        _logger.ErrorFormat("Job {0} not queued");
                        break;
                }
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
                    var rescheduleAt = DateTime.Now.AddSeconds(5*retries);
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
