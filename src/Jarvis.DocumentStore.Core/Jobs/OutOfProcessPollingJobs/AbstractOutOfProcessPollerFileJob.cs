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
using System.Net;
using System.Linq;
using Newtonsoft.Json;

namespace Jarvis.DocumentStore.Core.Jobs.PollingJobs
{
    public abstract class AbstractOutOfProcessPollerFileJob : IPollerJob
    {
        public bool IsOutOfProcess
        {
            get { return true; }
        }

        public String QueueName { get; protected set; }

        public PipelineId PipelineId { get; protected set; }

        public virtual bool IsActive { get { return true; } }

        public ILogger Logger { get; set; }

        public ConfigService ConfigService { get; set; }

        public DocumentStoreConfiguration DocumentStoreConfiguration { get; set; }

        public Boolean Started { get; private set; }

        private String _identity;

        private List<String> _documentStoreAddressUrls;

        public AbstractOutOfProcessPollerFileJob()
        {
            _identity = Environment.MachineName + "_" + System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        private List<String> _getNextJobUrlList;

        public void Start(List<String> documentStoreAddressUrls)
        {
            if (Started) return;
            _documentStoreAddressUrls = documentStoreAddressUrls;
            if (_documentStoreAddressUrls.Count == 0) throw new ArgumentException("Component needs at least a document store url", "documentStoreAddressUrls");
            _getNextJobUrlList = _documentStoreAddressUrls
                .Select(addr => addr.TrimEnd('/') + "/queue/getnextjob")
                .ToList();
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
                String pollerResult;
                using (WebClientEx client = new WebClientEx())
                {
                    //TODO: use round robin if a document store is down.
                    var firstUrl = _getNextJobUrlList.First();
                    var payload = JsonConvert.SerializeObject(new
                    {
                        QueueName = this.QueueName,
                        Identity = this._identity
                    });
                    Logger.DebugFormat("Polling url: {0} with payload {1}", firstUrl, payload);
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    pollerResult = client.UploadString(firstUrl, payload);
                    Logger.DebugFormat("GetNextJobResult: {0}", pollerResult);
                }
                //QueuedJob nextJob = null;
                //while ((nextJob = QueueDispatcher.GetNextJob(this.QueueName, _identity)) != null)
                //{
                //    try
                //    {
                //        PollerJobBaseParameters baseParameters = new PollerJobBaseParameters();
                //        baseParameters.FileExtension = nextJob.Parameters[JobKeys.FileExtension];
                //        baseParameters.InputDocumentId = new DocumentId(nextJob.Parameters[JobKeys.DocumentId]);
                //        baseParameters.InputDocumentFormat = new DocumentFormat(nextJob.Parameters[JobKeys.Format]);
                //        baseParameters.InputBlobId = new BlobId(nextJob.Parameters[JobKeys.BlobId]);
                //        baseParameters.TenantId = new TenantId(nextJob.Parameters[JobKeys.TenantId]);
                //        //remember to enter the right tenant.
                //        TenantContext.Enter(new TenantId(baseParameters.TenantId));
                //        var blobStore = TenantAccessor.GetTenant(baseParameters.TenantId).Container.Resolve<IBlobStore>();
                //        workingFolder = Path.Combine(
                //               ConfigService.GetWorkingFolder(baseParameters.TenantId, baseParameters.InputBlobId),
                //               GetType().Name
                //           );
                //        OnPolling(baseParameters, nextJob.Parameters, blobStore, workingFolder);
                //        QueueDispatcher.SetJobExecuted(this.QueueName, nextJob.Id, null);
                //    }
                //    catch (Exception ex)
                //    {
                //        Logger.ErrorFormat(ex, "Error executing queued job {0} on tenant {1}", nextJob.Id, nextJob.Parameters[JobKeys.TenantId]);
                //        QueueDispatcher.SetJobExecuted(this.QueueName, nextJob.Id, ex.Message);
                //    }
                //}
            }
            catch (Exception ex) 
            {
                Logger.ErrorFormat(ex, "Poller error: {0}", ex.Message);
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
            IDictionary<String, String> fullParameters, 
            IBlobStore currentTenantBlobStore,
            String workingFolder);
    }

}
