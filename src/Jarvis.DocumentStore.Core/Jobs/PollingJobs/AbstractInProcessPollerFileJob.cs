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
    public abstract class AbstractInProcessPollerFileJob : IPollerJob
    {

        public bool IsOutOfProcess
        {
            get { return false; }
        }

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

        public AbstractInProcessPollerFileJob()
        {
            _identity = Environment.MachineName + "_" + System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        public void Start(List<String> documentStoreAddressUrls)
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
                        PollerJobParameters parameters = new PollerJobParameters();
                        parameters.FileExtension = nextJob.Parameters[JobKeys.FileExtension];
                        parameters.InputDocumentId = new DocumentId(nextJob.Parameters[JobKeys.DocumentId]);
                        parameters.InputDocumentFormat = new DocumentFormat(nextJob.Parameters[JobKeys.Format]);
                        parameters.InputBlobId = new BlobId(nextJob.Parameters[JobKeys.BlobId]);
                        parameters.TenantId = new TenantId(nextJob.Parameters[JobKeys.TenantId]);
                        parameters.All = nextJob.Parameters;
                        //remember to enter the right tenant.
                        TenantContext.Enter(new TenantId(parameters.TenantId));
                        var blobStore = TenantAccessor.GetTenant(parameters.TenantId).Container.Resolve<IBlobStore>();
                        workingFolder = Path.Combine(
                               ConfigService.GetWorkingFolder(parameters.TenantId, parameters.InputBlobId),
                               GetType().Name
                           );
                        OnPolling(parameters, blobStore, workingFolder);
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

        protected abstract void OnPolling(PollerJobParameters parameters, IBlobStore currentTenantBlobStore, string workingFolder);
    }
}
