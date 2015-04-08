using System;
using System.Configuration;
using Castle.Facilities.Logging;
using Castle.Services.Logging.Log4netIntegration;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.DocumentStore.Host.Support;

namespace Jarvis.DocumentStore.Tests.Support
{
    public class DocumentStoreTestConfiguration : DocumentStoreConfiguration
    {
        public DocumentStoreTestConfiguration()
        {
            IsApiServer = true;
            IsWorker = false;
            IsReadmodelBuilder = true;

            QueueConnectionString = ConfigurationManager.ConnectionStrings["ds.queue"].ConnectionString;
            QueueInfoList = new Core.Jobs.QueueManager.QueueInfo[] { };

            QueueJobsPollInterval = 100; //poll each 100 milliseconds.
            QueueStreamPollInterval = 1000;
            IsQueueManager = false;
            TenantSettings.Add(new TestTenantSettings());

            Boost = "true";
            DelayedStartInMilliseconds = 1000;
            ForcedGcSecondsInterval = 0;
            EngineSlots = new String[] { "*" };
            PollingMsInterval = 100;
            AllowedFileTypes = "pdf|xls|xlsx|docx|doc|ppt|pptx|pps|ppsx|rtf|odt|ods|odp|htmlzip|eml|msg|jpeg|jpg|png|zip".Split('|');
            IsDeduplicationActive = true;
        }

        public override void CreateLoggingFacility(LoggingFacility f)
        {
            f.LogUsing<ExtendedConsoleLoggerFactory>();
        }

        public void SetTestAddress(Uri serverAddress)
        {
            AddServerAddress(serverAddress);
        }
    }

    public class DocumentStoreTestConfigurationForPollQueue : DocumentStoreTestConfiguration
    {
        public DocumentStoreTestConfigurationForPollQueue( QueueInfo[] queueInfo)
        {
            IsQueueManager = true;
            JobMode = JobModes.Queue;
            QueueJobsPollInterval = 50; //poll each 50 milliseconds.
            QueueStreamPollInterval = 50;
            
            this.QueueInfoList = queueInfo;
        }

        public override void CreateLoggingFacility(LoggingFacility f)
        {
            f.LogUsing<ExtendedConsoleLoggerFactory>();
        }
    }
}