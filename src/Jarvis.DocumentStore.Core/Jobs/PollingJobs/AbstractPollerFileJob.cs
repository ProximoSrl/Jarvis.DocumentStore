using System;
using System.IO;
using Castle.Core.Logging;
using CQRS.Shared.Commands;
using CQRS.Shared.MultitenantSupport;
using System.Timers;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Shared.Jobs;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using System.Collections.Generic;

namespace Jarvis.DocumentStore.Core.Jobs.PollingJobs
{
    public abstract class AbstractPollerFileJob : IPollerJob
    {

        public String QueueName { get; protected set; }

        public PipelineId PipelineId { get; protected set; }

        string _workingFolder;

        public ICommandBus CommandBus { get; set; }
        public ILogger Logger { get; set; }
        public IBlobStore BlobStore { get; set; }
        public ConfigService ConfigService { get; set; }

        IQueueDispatcher QueueDispatcher { get; set; }

        public void Start() { }

        //protected virtual Int32 NumOfParallelWorkers
        //{
        //    get { return 1; } //defaults to single worker.
        //}

        private Timer pollingTimer;

        public void Start(Int32 pollingTimeInMs)
        {
            pollingTimer = new Timer(pollingTimeInMs);
            pollingTimer.Elapsed += pollingTimer_Elapsed;
            pollingTimer.Start();
        }

        void pollingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            pollingTimer.Stop();
            try
            {
                var nextJob = QueueDispatcher.GetNextJob(this.QueueName);
                if (nextJob != null)
                {
                    PollerJobBaseParameters baseParameters = new PollerJobBaseParameters();
                    baseParameters.FileExtension = nextJob.Parameters[JobKeys.FileExtension];
                    baseParameters.InputDocumentId = new DocumentId( nextJob.Parameters[JobKeys.DocumentId]);
                    baseParameters.InputDocumentFormat = new DocumentFormat( nextJob.Parameters[JobKeys.Format]);
                    baseParameters.InputBlobId = new BlobId( nextJob.Parameters[JobKeys.BlobId]);
                    baseParameters.TenantId = new TenantId(nextJob.Parameters[JobKeys.TenantId]);
                    OnPolling(baseParameters, nextJob.Parameters);
                }
            }

            finally
            {
                pollingTimer.Start();
            }
        }

        protected abstract void OnPolling(PollerJobBaseParameters baseParameters, IDictionary<String, String> fullParameters);

        protected string DownloadFileToWorkingFolder(BlobId id)
        {
            return BlobStore.Download(id, _workingFolder);
        }

        protected string WorkingFolder
        {
            get { return _workingFolder; }
        }
    }

    public class PollerJobBaseParameters 
    {
        public DocumentId InputDocumentId { get; set; }
        public DocumentFormat InputDocumentFormat { get; set; }
        public BlobId InputBlobId { get; set; }
        public TenantId TenantId { get; set; }

        public String FileExtension { get; set; }
    }
}
