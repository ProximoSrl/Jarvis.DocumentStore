using System;
using System.Linq;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Shared.Commands;
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

        public void QueueThumbnail(PipelineId pipelineId, DocumentId documentId, BlobId blobId, string imageFormat)
        {
            var tenantId = TenantContext.CurrentTenantId;
            var job = GetJobForSingleTask<CreateThumbnailFromPdfJob>();
            var triggerBuilder = GetBuilderForTrigger(TimeSpan.Zero)
                .UsingJobData(JobKeys.TenantId, tenantId)
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.BlobId, blobId)
                .UsingJobData(JobKeys.PipelineId, pipelineId)
                .UsingJobData(JobKeys.FileExtension, imageFormat);
            ScheduleSingleJobWithMultipleTrigger<CreateThumbnailFromPdfJob>(job, triggerBuilder);
        }

       
        public void QueueEmailToHtml(PipelineId pipelineId, DocumentId documentId, BlobId blobId)
        {
            var tenantId = TenantContext.CurrentTenantId;
            var job = GetJobForSingleTask<AnalyzeEmailJob>();
            var triggerBuilder = GetBuilderForTrigger(TimeSpan.Zero)
                .UsingJobData(JobKeys.TenantId, tenantId)
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.BlobId, blobId)
                .UsingJobData(JobKeys.PipelineId, pipelineId);
            ScheduleSingleJobWithMultipleTrigger<AnalyzeEmailJob>(job, triggerBuilder);      
        }

        public void QueueResize(PipelineId pipelineId, DocumentId documentId, BlobId blobId,string imageFormat)
        {
            var tenantId = TenantContext.CurrentTenantId;
            var job = GetJobForSingleTask<ImageResizeJob>();
            var triggerBuilder = GetBuilderForTrigger(TimeSpan.Zero)
                .UsingJobData(JobKeys.TenantId, tenantId)
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.PipelineId, pipelineId)
                .UsingJobData(JobKeys.BlobId, blobId)
                .UsingJobData(JobKeys.FileExtension, imageFormat)
                .UsingJobData(JobKeys.Sizes, String.Join("|", _config.GetDefaultSizes().Select(x => x.Name)));
            ScheduleSingleJobWithMultipleTrigger<ImageResizeJob>(job, triggerBuilder);
        }

     
        public void QueueLibreOfficeToPdfConversion(PipelineId pipelineId, DocumentId documentId, BlobId blobId)
        {
            var tenantId = TenantContext.CurrentTenantId;
            var job = GetJobForSingleTask<LibreOfficeToPdfJob>();
            var triggerBuilder = GetBuilderForTrigger(TimeSpan.Zero)
                   .UsingJobData(JobKeys.TenantId, tenantId)
                   .UsingJobData(JobKeys.DocumentId, documentId)
                   .UsingJobData(JobKeys.PipelineId, pipelineId)
                   .UsingJobData(JobKeys.BlobId, blobId);
            job = ScheduleSingleJobWithMultipleTrigger<LibreOfficeToPdfJob>(job, triggerBuilder);
        }

        public void QueueHtmlToPdfConversion(PipelineId pipelineId, DocumentId documentId, BlobId blobId)
        {
            var tenantId = TenantContext.CurrentTenantId;
            var job = GetJobForSingleTask<HtmlToPdfJob>();
            var triggerBuilder = GetBuilderForTrigger(TimeSpan.Zero)
                   .UsingJobData(JobKeys.TenantId, tenantId)
                    .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.BlobId, blobId)
                .UsingJobData(JobKeys.PipelineId, pipelineId);
            ScheduleSingleJobWithMultipleTrigger<HtmlToPdfJob>(job, triggerBuilder);
        }

        public void QueueTikaAnalyzer(PipelineId pipelineId, DocumentId documentId, BlobId blobId)
        {
            var tenantId = TenantContext.CurrentTenantId;
            IJobDetail job;
            if (_config.UseEmbeddedTika)
                job = GetJobForSingleTask<ExtractTextWithTikaNetJob>();
            else
                job = GetJobForSingleTask<ExtractTextWithTikaJob>();

            var triggerBuilder = GetBuilderForTrigger(TimeSpan.Zero)
                .UsingJobData(JobKeys.TenantId, tenantId)
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.PipelineId, pipelineId)
                .UsingJobData(JobKeys.BlobId, blobId);

            if (_config.UseEmbeddedTika)
                ScheduleSingleJobWithMultipleTrigger<ExtractTextWithTikaNetJob>(job, triggerBuilder);
            else
                ScheduleSingleJobWithMultipleTrigger<ExtractTextWithTikaJob>(job, triggerBuilder);
        }

        IJobDetail GetJobForSingleTask<T>() where T : IJob
        {
            var jobKey = JobKey.Create(typeof(T).Name, typeof(T).Name);
            var job = _scheduler.GetJobDetail(jobKey);
            return job;
        }

        IJobDetail CreateJobForSingleTask<T>() where T : IJob
        {
            var jobKey = JobKey.Create(typeof(T).Name, typeof(T).Name);

            return JobBuilder
                .Create<T>()
                .WithIdentity(jobKey)
                .RequestRecovery(true)
                .StoreDurably(true)
                .Build();
        }

        JobBuilder GetBuilderForJob(Type jobType)
        {
            var tenantId = TenantContext.CurrentTenantId;

            return JobBuilder
                .Create(jobType)
                .UsingJobData(JobKeys.TenantId, tenantId)
                .RequestRecovery(true)
                .StoreDurably(true);
        }

        JobBuilder GetBuilderForJob<T>() where T : IJob
        {
            var tenantId = TenantContext.CurrentTenantId;

            return JobBuilder
                .Create<T>()
                .UsingJobData(JobKeys.TenantId, tenantId)
                .WithIdentity(JobKey.Create(Guid.NewGuid().ToString(), typeof(T).Name))
                .RequestRecovery(true)
                .StoreDurably(true);
        }


        ITrigger CreateTrigger()
        {
            return CreateTrigger(TimeSpan.Zero);
        }

        ITrigger CreateTrigger(TimeSpan delay)
        {
            var trigger = TriggerBuilder.Create()
                .StartAt(DateTimeOffset.Now + delay)
                .Build();
            return trigger;
        }

        TriggerBuilder GetBuilderForTrigger(TimeSpan delay)
        {
            return TriggerBuilder.Create()
                .StartAt(DateTimeOffset.Now + delay);
        }

        /// <summary>
        /// Schedule the job with given trigger. If the job is null it will be created and scheduled for the first time.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="job"></param>
        /// <param name="triggerBuilder"></param>
        /// <returns></returns>
        private IJobDetail ScheduleSingleJobWithMultipleTrigger<T>(IJobDetail job, TriggerBuilder triggerBuilder) where T : IJob
        {
            if (job == null)
            {
                //No existing job, create one
                job = CreateJobForSingleTask<T>();
                var trigger = triggerBuilder.Build();

                _scheduler.ScheduleJob(job, trigger);
            }
            else
            {
                //Job existing, simply add trigger.
                var trigger = triggerBuilder
                   .ForJob(job)
                   .Build();

                _scheduler.ScheduleJob(trigger);
            }
            return job;
        }

    }
}