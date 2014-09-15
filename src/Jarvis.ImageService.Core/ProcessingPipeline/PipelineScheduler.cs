using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Jobs;
using Quartz;

namespace Jarvis.ImageService.Core.ProcessingPipeline
{
    public interface IPipelineScheduler
    {
        void QueueThumbnail(string documentId);
    }

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
                .WithIdentity(documentId +".thumbnail.job")
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity(documentId + ".thumbnail.trigger")
                .UsingJobData(CreateThumbnailFromPdfJob.Documentid, documentId)
                .StartNow()
                .Build();

            Scheduler.ScheduleJob(job, trigger);
        }
    }
}
