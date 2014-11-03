using System;
using System.IO;
using Castle.Core.Logging;
using CQRS.Shared.Commands;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public abstract class AbstractFileJob : ITenantJob
    {
        string _workingFolder;
        protected DocumentId DocumentId { get; private set; }
        protected FileId FileId { get; private set; }
        protected PipelineId PipelineId { get; private set; }
        public TenantId TenantId { get; set; }

        public ICommandBus CommandBus { get; set; }
        public ILogger Logger { get; set; }
        public IFileStore FileStore { get; set; }
        public ConfigService ConfigService { get; set; }
        
        public void Execute(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            DocumentId = new DocumentId(jobDataMap.GetString(JobKeys.DocumentId));
            FileId = new FileId(jobDataMap.GetString(JobKeys.FileId));
            PipelineId = new PipelineId(jobDataMap.GetString(JobKeys.PipelineId));

            if (TenantId == null)
                throw new Exception("tenant not set!");

            _workingFolder = Path.Combine(
                ConfigService.GetWorkingFolder(TenantId, FileId), 
                GetType().Name
            );
            
            OnExecute(context);

            try
            {
                if (Directory.Exists(_workingFolder))
                    Directory.Delete(_workingFolder,true);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat(ex, "Error deleting {0}", _workingFolder);
            }
        }

        protected abstract void OnExecute(IJobExecutionContext context);

        protected string DownloadFileToWorkingFolder(FileId id)
        {
            return FileStore.Download(id, _workingFolder);
        }

        protected string WorkingFolder
        {
            get { return _workingFolder; }
        }
    }
}