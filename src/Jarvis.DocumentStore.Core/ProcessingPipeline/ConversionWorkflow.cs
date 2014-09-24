using System;
using System.Linq;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Services;
using Quartz;

namespace Jarvis.DocumentStore.Core.ProcessingPipeline
{
    public class ConversionWorkflow : IConversionWorkflow
    {
        const string ResizeJobId = "resize";
        const string ThumbnailJobId = "thumbnail";
        
        readonly IScheduler _scheduler;
        public ILogger Logger { get; set; }
        readonly IFileService _fileService;
        const string ImageFormat = "png";

        public ConversionWorkflow(IScheduler scheduler, IFileService fileService)
        {
            _scheduler = scheduler;
            _fileService = fileService;
        }

        public void QueueThumbnail(FileInfo fileInfo)
        {
            var job = GetBuilderForJob<CreateThumbnailFromPdfJob>()
                .UsingJobData(JobKeys.FileId, fileInfo.Id)
                .UsingJobData(JobKeys.FileExtension, ImageFormat)
                .UsingJobData(JobKeys.NextJob, ResizeJobId)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void QueueResize(FileInfo fileInfo)
        {
            var job = GetBuilderForJob<ImageResizeJob>()
                .UsingJobData(JobKeys.FileId, fileInfo.Id)
                .UsingJobData(JobKeys.FileExtension, ImageFormat)
                .UsingJobData(JobKeys.Sizes, String.Join("|", fileInfo.Sizes.Select(x => x.Key)))
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void Next(FileId fileId, string nextJob)
        {
            var fileInfo = _fileService.GetById(fileId);

            switch (nextJob)
            {
                case ThumbnailJobId: QueueThumbnail(fileInfo);
                    break;

                case ResizeJobId: QueueResize(fileInfo);
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

        public void QueueLibreOfficeToPdfConversion(FileInfo fileInfo)
        {
            var fileExtension = fileInfo.GetFileExtension();

            var job = GetBuilderForJob<LibreOfficeToPdfJob>()
                .UsingJobData(JobKeys.FileId, fileInfo.Id)
                .UsingJobData(JobKeys.FileExtension, fileExtension)
                .UsingJobData(JobKeys.NextJob, ThumbnailJobId)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void QueueHtmlToPdfConversion(FileInfo fileInfo)
        {
            var job = GetBuilderForJob<HtmlToPdfJob>()
                .UsingJobData(JobKeys.FileId, fileInfo.Id)
                .UsingJobData(JobKeys.NextJob, ThumbnailJobId)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void Start(FileId fileId)
        {
            var fileInfo = _fileService.GetById(fileId);

            switch (fileInfo.GetFileExtension())
            {
                case ".pdf":
                    QueueThumbnail(fileInfo);
                    break;

                case ".htmlzip":
                    QueueHtmlToPdfConversion(fileInfo);
                    break;

                default:
                    QueueLibreOfficeToPdfConversion(fileInfo);
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
