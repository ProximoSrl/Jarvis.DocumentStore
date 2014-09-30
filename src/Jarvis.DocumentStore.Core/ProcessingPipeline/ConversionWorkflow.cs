using System;
using System.Linq;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Quartz;

namespace Jarvis.DocumentStore.Core.ProcessingPipeline
{
    public class ConversionWorkflow : IConversionWorkflow
    {
        const string ResizeJobId = "resize";
        const string ThumbnailJobId = "thumbnail";
        
        readonly IScheduler _scheduler;
        public ILogger Logger { get; set; }
        readonly IFileStore _fileStore;
        const string ImageFormat = "png";
        ConfigService _config;

        public ConversionWorkflow(IScheduler scheduler, ConfigService config, IFileStore fileStore)
        {
            _scheduler = scheduler;
            _config = config;
            _fileStore = fileStore;
        }

        public void QueueThumbnail(FileId fileId)
        {
            var job = GetBuilderForJob<CreateThumbnailFromPdfJob>()
                .UsingJobData(JobKeys.FileId, fileId)
                .UsingJobData(JobKeys.FileExtension, ImageFormat)
                .UsingJobData(JobKeys.NextJob, ResizeJobId)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void QueueResize(FileId fileId)
        {
            var job = GetBuilderForJob<ImageResizeJob>()
                .UsingJobData(JobKeys.FileId, fileId)
                .UsingJobData(JobKeys.FileExtension, ImageFormat)
                .UsingJobData(JobKeys.Sizes, String.Join("|", _config.GetDefaultSizes().Select(x => x.Name)))
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void Next(FileId fileId, string nextJob)
        {
            switch (nextJob)
            {
                case ThumbnailJobId: QueueThumbnail(fileId);
                    break;

                case ResizeJobId: QueueResize(fileId);
                    break;

                default:
                    Logger.ErrorFormat("Job {0} not queued");
                    break;
            }
        }

        JobBuilder GetBuilderForJob<T>() where T : IJob
        {
            return JobBuilder
                .Create<T>()
                .RequestRecovery(true)
                .StoreDurably(false);
        }

        public void QueueLibreOfficeToPdfConversion(FileId fileId)
        {

            var job = GetBuilderForJob<LibreOfficeToPdfJob>()
                .UsingJobData(JobKeys.FileId, fileId)
                .UsingJobData(JobKeys.NextJob, ThumbnailJobId)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void QueueHtmlToPdfConversion(FileId fileId)
        {
            var job = GetBuilderForJob<HtmlToPdfJob>()
                .UsingJobData(JobKeys.FileId, fileId)
                .UsingJobData(JobKeys.NextJob, ThumbnailJobId)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void Start(FileId fileId)
        {
            var descriptor = _fileStore.GetDescriptor(fileId);

            switch (descriptor.FileExtension)
            {
                case ".pdf":
                    QueueThumbnail(fileId);
                    break;

                case ".htmlzip":
                    QueueHtmlToPdfConversion(fileId);
                    break;

                default:
                    QueueLibreOfficeToPdfConversion(fileId);
                    break;
            }
        }

        private ITrigger CreateTrigger()
        {
            var trigger = TriggerBuilder.Create()
#if DEBUG
                .StartAt(DateTimeOffset.Now.AddSeconds(15))
#else
                .StartAt(DateTimeOffset.Now)
#endif
.Build();
            return trigger;
        }
    }
}
