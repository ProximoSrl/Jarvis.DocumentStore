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

        private String _handle;

        public AbstractInProcessPollerFileJob()
        {
            _identity = Environment.MachineName + "_" + System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        public void Start(List<String> documentStoreAddressUrls, String handle)
        {
            if (Started) return;
            _handle = handle;
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
                QueuedJobDto nextJob = null;
                while ((nextJob = QueueDispatcher.GetNextJob(this.QueueName, _identity, _handle, null, null)) != null)
                {
                    try
                    {
                        PollerJobParameters parameters = new PollerJobParameters();
                        parameters.FileExtension = nextJob.Parameters[JobKeys.FileExtension];
                        parameters.JobId = new QueuedJobId(nextJob.Id);
                        parameters.InputDocumentFormat = new DocumentFormat(nextJob.Parameters[JobKeys.Format]);
                        parameters.TenantId = new TenantId(nextJob.Parameters[JobKeys.TenantId]);
                        parameters.All = nextJob.Parameters;
                        //remember to enter the right tenant.
                        TenantContext.Enter(new TenantId(parameters.TenantId));
                        var blobStore = TenantAccessor.GetTenant(parameters.TenantId).Container.Resolve<IBlobStore>();
                        workingFolder = Path.Combine(
                               ConfigService.GetWorkingFolder(parameters.TenantId, parameters.JobId),
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
                    finally 
                    {
                        DeleteWorkingFolder(workingFolder);
                    }
                }
            }

            finally
            {
               
                if (Started && pollingTimer != null) pollingTimer.Start();
                
            }
        }

        private void DeleteWorkingFolder(String workingFolder)
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
        }

        protected abstract void OnPolling(PollerJobParameters parameters, IBlobStore currentTenantBlobStore, string workingFolder);
    }
}
