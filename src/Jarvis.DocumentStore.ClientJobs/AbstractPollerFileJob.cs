using System;
using System.IO;
using Castle.Core.Logging;
using CQRS.Shared.Commands;
using CQRS.Shared.MultitenantSupport;
using System.Timers;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.ClientJobs
{
    public class AbstractPollerFileJob
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

        public void Start() { }

        protected virtual Int32 NumOfParallelWorkers
        {
            get { return 1; } //defaults to single worker.
        }

        private Timer pollingTimer;

        public void Start(Int32 pollingTimeInMs)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            PipelineId = new PipelineId(jobDataMap.GetString(JobKeys.PipelineId));

            InputDocumentId = new DocumentId(jobDataMap.GetString(JobKeys.DocumentId));
            InputBlobId = new BlobId(jobDataMap.GetString(JobKeys.BlobId));
            InputDocumentFormat = new DocumentFormat(jobDataMap.GetString(JobKeys.Format));

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
                    Directory.Delete(_workingFolder, true);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat(ex, "Error deleting {0}", _workingFolder);
            }

            pollingTimer = new Timer(pollingTimeInMs);
            pollingTimer.Elapsed += pollingTimer_Elapsed;
            pollingTimer.Start();
        }

        void pollingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            pollingTimer.Stop();
            try
            {

            }
            finally
            {
                pollingTimer.Start();
            }
        }



        protected abstract void OnPolling();


    }
}
