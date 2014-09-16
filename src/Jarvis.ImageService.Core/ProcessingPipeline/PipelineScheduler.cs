using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Jobs;
using Quartz;

namespace Jarvis.ImageService.Core.ProcessingPipeline
{
    public class PipelineScheduler : IPipelineScheduler
    {
        public PipelineScheduler(IScheduler scheduler)
        {
            Scheduler = scheduler;
        }

        private IScheduler Scheduler { get; set; }

        public void QueueThumbnail(string documentId)
        {
            var job = JobBuilder
                .Create<CreateThumbnailFromPdfJob>()
                .UsingJobData(CreateThumbnailFromPdfJob.Documentid, documentId)
                .StoreDurably(true)
                .Build();

            var trigger = TriggerBuilder.Create()
                .StartAt(DateTimeOffset.Now.AddSeconds(15))
                .Build();


            Scheduler.ScheduleJob(job, trigger);
        }
    }
}
