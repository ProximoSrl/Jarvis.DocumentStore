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

        public void QueueCommand(ICommand command, string asUser)
        {
            var jobType = typeof (CommandRunnerJob<>).MakeGenericType(new[] {command.GetType()});
            var job = GetBuilderForJob(jobType)
                .UsingJobData(JobKeys.Command, CommandSerializer.Serialize(command))
                .Build();

            if (!job.JobDataMap.ContainsKey(JobKeys.TenantId))
            {
                throw new Exception("Command needs a tenant context");
            }

            var trigger = CreateTrigger(TimeSpan.FromDays(-1));
            trigger.Priority = 100;
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
            var tenantId = TenantContext.CurrentTenantId;

            return JobBuilder
                .Create<T>()
                .UsingJobData(JobKeys.TenantId, tenantId)
                .WithIdentity(JobKey.Create(Guid.NewGuid().ToString(), typeof(T).Name))
                .RequestRecovery(true)
                .StoreDurably(true);
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
            return CreateTrigger(TimeSpan.Zero);
        }

        ITrigger CreateTrigger(TimeSpan delay)
        {
            var trigger = TriggerBuilder.Create()
                .StartAt(DateTimeOffset.Now + delay)
                .Build();
            return trigger;
        }
    }
}