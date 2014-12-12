﻿using System;
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
        protected DocumentId InputDocumentId { get; private set; }
        protected DocumentFormat InputDocumentFormat { get; private set; }
        protected BlobId InputBlobId { get; private set; }
        protected PipelineId PipelineId { get; private set; }
        public TenantId TenantId { get; set; }

        public ICommandBus CommandBus { get; set; }
        public ILogger Logger { get; set; }
        public IBlobStore BlobStore { get; set; }
        public ConfigService ConfigService { get; set; }
        
        public void Execute(IJobExecutionContext context)
        {
            var jobDataMap = context.MergedJobDataMap;
            PipelineId = new PipelineId(jobDataMap.GetString(JobKeys.PipelineId));

            InputDocumentId = new DocumentId(jobDataMap.GetString(JobKeys.DocumentId));
            InputBlobId = new BlobId(jobDataMap.GetString(JobKeys.BlobId));
            InputDocumentFormat = new DocumentFormat(jobDataMap.GetString(JobKeys.Format));
            TenantId = TenantId ?? new TenantId(jobDataMap.GetString(JobKeys.TenantId));
            if (TenantId == null)
                throw new Exception("tenant not set!");

            _workingFolder = Path.Combine(
                ConfigService.GetWorkingFolder(TenantId, InputBlobId), 
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

        protected string DownloadFileToWorkingFolder(BlobId id)
        {
            return BlobStore.Download(id, _workingFolder);
        }

        protected string WorkingFolder
        {
            get { return _workingFolder; }
        }
    }
}