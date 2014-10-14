using System;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using CQRS.Shared.Commands;
using CQRS.Shared.Domain.Serialization;
using CQRS.Shared.Messages;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Services;
using Newtonsoft.Json;
using Quartz;

namespace Jarvis.DocumentStore.Core.Processing
{
    public static class CommandSerializer
    {
        private static JsonSerializerSettings _settings;

        static CommandSerializer ()
        {
            _settings = new JsonSerializerSettings()
            {
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
                Converters = new JsonConverter[]
                {
                    new StringValueJsonConverter()
                },
                ContractResolver = new MessagesContractResolver(),
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
            };
        }

        public static string Serialize(ICommand command)
        {
            return JsonConvert.SerializeObject(command, _settings);
        }

        public static T Deserialize<T>(string command) where T : ICommand
        {
            return JsonConvert.DeserializeObject<T>(command, _settings);
        }
    }

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
            return JobBuilder
                .Create<T>()
                .WithIdentity(JobKey.Create(Guid.NewGuid().ToString(), typeof(T).Name))
                .RequestRecovery(true)
                .StoreDurably(true);
        }

        JobBuilder GetBuilderForJob(Type jobType) 
        {
            return JobBuilder
                .Create(jobType)
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