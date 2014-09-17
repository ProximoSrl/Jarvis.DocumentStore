using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Jobs;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.ProcessingPipeline.Conversions;
using Quartz;

namespace Jarvis.ImageService.Core.ProcessingPipeline
{
    public class PipelineScheduler : IPipelineScheduler
    {
        readonly IScheduler _scheduler;

        public PipelineScheduler(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public void QueueThumbnail(ImageInfo imageInfo)
        {
            var job = JobBuilder
                .Create<CreateThumbnailFromPdfJob>()
                .UsingJobData(CreateThumbnailFromPdfJob.FileIdKey, imageInfo.Id)
                .UsingJobData(CreateThumbnailFromPdfJob.SizesKey, String.Join("|",imageInfo.Sizes.Select(x=>x.Key)))
                .StoreDurably(true)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void QueuePdfConversion(ImageInfo imageInfo)
        {
            var job = JobBuilder
                .Create<ConvertToPdfJob>()
                .UsingJobData(ConvertToPdfJob.FileIdKey, imageInfo.Id)
                .UsingJobData(ConvertToPdfJob.FileExtensionKey, imageInfo.GetFileExtension())
                .StoreDurably(true)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        private ITrigger CreateTrigger()
        {
            var trigger = TriggerBuilder.Create()
                .StartAt(DateTimeOffset.Now.AddSeconds(15))
                .Build();
            return trigger;
        }
    }
}
