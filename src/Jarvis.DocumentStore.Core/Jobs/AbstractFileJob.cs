using Castle.Core.Logging;
using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public abstract class AbstractFileJob : IJob
    {
        protected DocumentId DocumentId { get; private set; }
        protected FileId FileId { get; private set; }

        public ICommandBus CommandBus { get; set; }
        public ILogger Logger { get; set; }
        public IFileStore FileStore { get; set; }

        public void Execute(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            DocumentId = new DocumentId(jobDataMap.GetString(JobKeys.DocumentId));
            FileId = new FileId(jobDataMap.GetString(JobKeys.FileId));
            
            OnExecute(context);
        }

        protected abstract void OnExecute(IJobExecutionContext context);
    }
}