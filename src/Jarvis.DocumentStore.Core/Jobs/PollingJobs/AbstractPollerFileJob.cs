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
using Jarvis.DocumentStore.Core.Support;
using CQRS.Kernel.MultitenantSupport;

namespace Jarvis.DocumentStore.Core.Jobs.PollingJobs
{
    public abstract class AbstractPollerFileJob : IPollerJob
    {

        public String QueueName { get; protected set; }

        public PipelineId PipelineId { get; protected set; }

        public virtual bool IsActive { get { return true; } }

        public ICommandBus CommandBus { get; set; }

        public ITenantAccessor TenantAccessor { get; set; }

        public ILogger Logger { get; set; }

        public ConfigService ConfigService { get; set; }

        public DocumentStoreConfiguration DocumentStoreConfiguration { get; set; }

        public IQueueDispatcher QueueDispatcher { get; set; }

        public Boolean Started { get; private set; }

        private String _identity;

        public AbstractPollerFileJob()
        {
            _identity = Environment.MachineName + "_" + System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        public void Start()
        {
            if (Started) return;
            Start(DocumentStoreConfiguration.QueueStreamPollInterval);
            Started = true;
        }

        public void Stop()
        {
            if (!Started) return;
            Started = false;
            pollingTimer.Dispose();
            pollingTimer = null;
        }

        //protected virtual Int32 NumOfParallelWorkers
        //{
        //    get { return 1; } //defaults to single worker.
        //}

        private Timer pollingTimer;

        private void Start(Int32 pollingTimeInMs)
        {
            pollingTimer = new Timer(pollingTimeInMs);
            pollingTimer.Elapsed += pollingTimer_Elapsed;
            pollingTimer.Start();
        }

        void pollingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            pollingTimer.Stop();
            String workingFolder = null;
            try
            {
                QueuedJob nextJob = null;
                while ((nextJob = QueueDispatcher.GetNextJob(this.QueueName, _identity)) != null)
                {
                    try
                    {
                        PollerJobBaseParameters baseParameters = new PollerJobBaseParameters();
                        baseParameters.FileExtension = nextJob.Parameters[JobKeys.FileExtension];
                        baseParameters.InputDocumentId = new DocumentId(nextJob.Parameters[JobKeys.DocumentId]);
                        baseParameters.InputDocumentFormat = new DocumentFormat(nextJob.Parameters[JobKeys.Format]);
                        baseParameters.InputBlobId = new BlobId(nextJob.Parameters[JobKeys.BlobId]);
                        baseParameters.TenantId = new TenantId(nextJob.Parameters[JobKeys.TenantId]);
                        //remember to enter the right tenant.
                        TenantContext.Enter(new TenantId(baseParameters.TenantId));
                        var blobStore = TenantAccessor.GetTenant(baseParameters.TenantId).Container.Resolve<IBlobStore>();
                        workingFolder = Path.Combine(
                               ConfigService.GetWorkingFolder(baseParameters.TenantId, baseParameters.InputBlobId),
                               GetType().Name
                           );
                        OnPolling(baseParameters, nextJob.Parameters, blobStore, workingFolder);
                        QueueDispatcher.SetJobExecuted(this.QueueName, nextJob.Id, null);
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorFormat(ex, "Error executing queued job {0} on tenant {1}", nextJob.Id, nextJob.Parameters[JobKeys.TenantId]);
                        QueueDispatcher.SetJobExecuted(this.QueueName, nextJob.Id, ex.Message);
                    }
                }
            }

            finally
            {
                if (!String.IsNullOrEmpty(workingFolder)) 
                {
                    try
                    {
                        if (Directory.Exists(workingFolder))
                            Directory.Delete(workingFolder, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorFormat(ex, "Error deleting {0}", workingFolder);
                    }
                }
                if (Started && pollingTimer != null) pollingTimer.Start();
                
            }
        }

        protected abstract void OnPolling(
            PollerJobBaseParameters baseParameters, 
            IDictionary<String, String> fullParameters, 
            IBlobStore currentTenantBlobStore,
            String workingFolder);
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
