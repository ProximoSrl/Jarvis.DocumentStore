using System;
using System.Linq;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Quartz;

namespace Jarvis.DocumentStore.Core.ProcessingPipeline
{
    public static class DocumentFormats
    {
        public const string RasterImage = "raster";
        public const string Pdf = "pdf";
        public const string Original = "original";
    }

    public class ConversionWorkflow : IConversionWorkflow
    {
        readonly IScheduler _scheduler;
        public ILogger Logger { get; set; }
        readonly IFileStore _fileStore;
        const string ImageFormat = "png";
        readonly ConfigService _config;

        public ConversionWorkflow(IScheduler scheduler, ConfigService config, IFileStore fileStore)
        {
            _scheduler = scheduler;
            _config = config;
            _fileStore = fileStore;
        }

        void QueueThumbnail(DocumentId documentId, FileId fileId)
        {
            var job = GetBuilderForJob<CreateThumbnailFromPdfJob>()
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.FileId, fileId)
                .UsingJobData(JobKeys.FileExtension, ImageFormat)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        void QueueResize(DocumentId documentId, FileId fileId)
        {
            var job = GetBuilderForJob<ImageResizeJob>()
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.FileId, fileId)
                .UsingJobData(JobKeys.FileExtension, ImageFormat)
                .UsingJobData(JobKeys.Sizes, String.Join("|", _config.GetDefaultSizes().Select(x => x.Name)))
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void FormatAvailable(DocumentId documentId, DocumentFormat format, FileId fileId)
        {
            switch (format)
            {
                case DocumentFormats.Pdf: QueueThumbnail(documentId, fileId);
                    break;

                case DocumentFormats.RasterImage: QueueResize(documentId,fileId);
                    break;

                default:
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

        void QueueLibreOfficeToPdfConversion(DocumentId documentId, FileId fileId)
        {
            var job = GetBuilderForJob<LibreOfficeToPdfJob>()
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.FileId, fileId)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        void QueueHtmlToPdfConversion(DocumentId documentId, FileId fileId)
        {
            var job = GetBuilderForJob<HtmlToPdfJob>()
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.FileId, fileId)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        void QueueTikaAnalyzer(DocumentId documentId, FileId fileId)
        {
            var job = GetBuilderForJob<ExtractTextWithTikaJob>()
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.FileId, fileId)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void Start(DocumentId documentId, FileId fileId)
        {
            var descriptor = _fileStore.GetDescriptor(fileId);

            switch (descriptor.FileExtension)
            {
                case ".pdf":
                    QueueThumbnail(documentId, fileId);
                    QueueTikaAnalyzer(documentId, fileId);
                    break;

                case ".htmlzip":
                    QueueHtmlToPdfConversion(documentId,fileId);
                    break;

                default:
                    QueueLibreOfficeToPdfConversion(documentId, fileId);
                    QueueTikaAnalyzer(documentId, fileId);
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
