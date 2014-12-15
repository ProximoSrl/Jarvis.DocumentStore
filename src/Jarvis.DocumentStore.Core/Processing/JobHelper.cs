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
            var job = GetBuilderForJob<CreateThumbnailFromPdfJob>()
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.BlobId, blobId)
                .UsingJobData(JobKeys.PipelineId, pipelineId)
                .UsingJobData(JobKeys.FileExtension, imageFormat)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void QueueEmailToHtml(PipelineId pipelineId, DocumentId documentId, BlobId blobId)
        {
            var job = GetBuilderForJob<AnalyzeEmailJob>()
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.BlobId, blobId)
                .UsingJobData(JobKeys.PipelineId, pipelineId)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);        
        }

        public void QueueResize(PipelineId pipelineId, DocumentId documentId, BlobId blobId,string imageFormat)
        {
            var job = GetBuilderForJob<ImageResizeJob>()
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.PipelineId, pipelineId)
                .UsingJobData(JobKeys.BlobId, blobId)
                .UsingJobData(JobKeys.FileExtension, imageFormat)
                .UsingJobData(JobKeys.Sizes, String.Join("|", _config.GetDefaultSizes().Select(x => x.Name)))
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
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
            if (job == null)
            {
                //No existing job, create one
                job = CreateJobForSingleTask<LibreOfficeToPdfJob>();
                var trigger = triggerBuilder.Build();

                _scheduler.ScheduleJob(job, trigger);
            }
            else
            { 
                //Job existing
                var trigger = triggerBuilder
                   .ForJob(job)
                   .Build();

                _scheduler.ScheduleJob(trigger);
            }
        }

        public void QueueHtmlToPdfConversion(PipelineId pipelineId, DocumentId documentId, BlobId blobId)
        {
            var job = GetBuilderForJob<HtmlToPdfJob>()
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.BlobId, blobId)
                .UsingJobData(JobKeys.PipelineId, pipelineId)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void QueueTikaAnalyzer(PipelineId pipelineId, DocumentId documentId, BlobId blobId)
        {
            JobBuilder builder = null;
            if(_config.UseEmbeddedTika)
                builder = GetBuilderForJob<ExtractTextWithTikaNetJob>();
            else
                builder = GetBuilderForJob<ExtractTextWithTikaJob>();

            var job = builder
                .UsingJobData(JobKeys.DocumentId, documentId)
                .UsingJobData(JobKeys.PipelineId, pipelineId)
                .UsingJobData(JobKeys.BlobId, blobId)
                .Build();

            var trigger = CreateTrigger();

            _scheduler.ScheduleJob(job, trigger);
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
    }
}