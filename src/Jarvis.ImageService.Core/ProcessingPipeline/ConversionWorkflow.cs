using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.ImageService.Core.Jobs;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.Services;
using Quartz;

namespace Jarvis.ImageService.Core.ProcessingPipeline
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
            var job = JobBuilder
                .Create<CreateThumbnailFromPdfJob>()
                .UsingJobData(JobKeys.FileId, fileInfo.Id)
                .UsingJobData(JobKeys.FileExtension, ImageFormat)
                .UsingJobData(JobKeys.NextJob, ResizeJobId)
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
                .UsingJobData(JobKeys.FileExtension, ImageFormat)
                .UsingJobData(JobKeys.Sizes, String.Join("|", fileInfo.Sizes.Select(x => x.Key)))
                .StoreDurably(true)
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

        public void QueueLibreOfficeToPdfConversion(FileInfo fileInfo)
        {
            var fileExtension = fileInfo.GetFileExtension();

            var job = JobBuilder
                .Create<LibreOfficeToPdfJob>()
                .UsingJobData(JobKeys.FileId, fileInfo.Id)
                .UsingJobData(JobKeys.FileExtension, fileExtension)
                .UsingJobData(JobKeys.NextJob, ThumbnailJobId)
                .StoreDurably(true)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void QueueHtmlToPdfConversion(FileInfo fileInfo)
        {
            var job = JobBuilder
                .Create<HtmlToPdfJob>()
                .UsingJobData(JobKeys.FileId, fileInfo.Id)
                .UsingJobData(JobKeys.NextJob, ThumbnailJobId)
                .StoreDurably(true)
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
                .StartAt(DateTimeOffset.Now)
                .Build();
            return trigger;
        }
    }
}
