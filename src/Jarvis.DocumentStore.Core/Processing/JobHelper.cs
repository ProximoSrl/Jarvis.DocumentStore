using System;
using System.Linq;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Services;
using Quartz;

namespace Jarvis.DocumentStore.Core.Processing
{
    public class JobHelper : IJobHelper
    {
        readonly IScheduler _scheduler;
        readonly ConfigService _config;

        public JobHelper(IScheduler scheduler, ConfigService config)
        {
            _scheduler = scheduler;
            _config = config;
        }

        public void QueueThumbnail(PipelineId pipelineId, DocumentId documentId, FileId fileId, string imageFormat)
        {
            var job = GetBuilderForJob<CreateThumbnailFromPdfJob>()
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.FileId, fileId)
                .UsingJobData(JobKeys.PipelineId, pipelineId)
                .UsingJobData(JobKeys.FileExtension, imageFormat)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void QueueEmailToHtml(PipelineId pipelineId, DocumentId documentId, FileId fileId)
        {
            var job = GetBuilderForJob<AnalyzeEmailJob>()
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.FileId, fileId)
                .UsingJobData(JobKeys.PipelineId, pipelineId)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);        
        }

        public void QueueResize(PipelineId pipelineId, DocumentId documentId, FileId fileId,string imageFormat)
        {
            var job = GetBuilderForJob<ImageResizeJob>()
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.PipelineId, pipelineId)
                .UsingJobData(JobKeys.FileId, fileId)
                .UsingJobData(JobKeys.FileExtension, imageFormat)
                .UsingJobData(JobKeys.Sizes, String.Join("|", _config.GetDefaultSizes().Select(x => x.Name)))
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        JobBuilder GetBuilderForJob<T>() where T : IJob
        {
            return JobBuilder
                .Create<T>()
                .RequestRecovery(true)
                .StoreDurably(true);
        }

        public void QueueLibreOfficeToPdfConversion(PipelineId pipelineId, DocumentId documentId, FileId fileId)
        {
            var job = GetBuilderForJob<LibreOfficeToPdfJob>()
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.PipelineId, pipelineId)
                .UsingJobData(JobKeys.FileId, fileId)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void QueueHtmlToPdfConversion(PipelineId pipelineId, DocumentId documentId, FileId fileId)
        {
            var job = GetBuilderForJob<HtmlToPdfJob>()
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.FileId, fileId)
                .UsingJobData(JobKeys.PipelineId, pipelineId)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void QueueTikaAnalyzer(PipelineId pipelineId, DocumentId documentId, FileId fileId)
        {
            var job = GetBuilderForJob<ExtractTextWithTikaJob>()
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.PipelineId, pipelineId)
                .UsingJobData(JobKeys.FileId, fileId)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        ITrigger CreateTrigger()
        {
            var trigger = TriggerBuilder.Create()
                .StartAt(DateTimeOffset.Now)
                .Build();
            return trigger;
        }
    }
}