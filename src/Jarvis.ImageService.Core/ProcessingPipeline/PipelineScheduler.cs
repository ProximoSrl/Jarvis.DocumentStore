using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.ImageService.Core.Jobs;
using Jarvis.ImageService.Core.Model;
using Quartz;

namespace Jarvis.ImageService.Core.ProcessingPipeline
{
    public class PipelineScheduler : IPipelineScheduler
    {
        readonly IScheduler _scheduler;
        public ILogger Logger { get; set; }

        public PipelineScheduler(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public void QueueThumbnail(FileInfo fileInfo)
        {
            var job = JobBuilder
                .Create<CreateThumbnailFromPdfJob>()
                .UsingJobData(JobKeys.FileId, fileInfo.Id)
                .UsingJobData(JobKeys.NextJob, "resize")
                .StoreDurably(true)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void QueueResize(FileInfo fileInfo)
        {
            var job = JobBuilder
                .Create<ImageResizeJob>()
                .UsingJobData(JobKeys.FileId, fileInfo.Id)
                .UsingJobData(JobKeys.Sizes, String.Join("|", fileInfo.Sizes.Select(x => x.Key)))
                .StoreDurably(true)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void QueuePdfConversion(FileInfo fileInfo)
        {
            var fileExtension = fileInfo.GetFileExtension();

            var job = JobBuilder
                .Create<ConvertToPdfJob>()
                .UsingJobData(JobKeys.FileId, fileInfo.Id)
                .UsingJobData(JobKeys.FileExtension, fileExtension)
                .UsingJobData(JobKeys.NextJob, "thumbnail")
                .StoreDurably(true)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void QueueHtmlToPdfConversion(FileInfo fileInfo)
        {
            var job = JobBuilder
                .Create<ConvertHtmlToPdfJob>()
                .UsingJobData(JobKeys.FileId, fileInfo.Id)
                .UsingJobData(JobKeys.NextJob, "thumbnail")
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
