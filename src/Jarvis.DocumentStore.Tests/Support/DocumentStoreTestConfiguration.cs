using System;
using System.Configuration;
using Castle.Facilities.Logging;
using Castle.Services.Logging.Log4netIntegration;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.Framework.Kernel.ProjectionEngine;
using System.Collections.Generic;

namespace Jarvis.DocumentStore.Tests.Support
{
    public class DocumentStoreTestConfiguration : DocumentStoreConfiguration
    {
        public DocumentStoreTestConfiguration(String engineVersion = "v3", String tenantId = "tests")
        {
            if (engineVersion != "v3") throw new NotSupportedException("Only v3 is supported with this version of NES");
            EngineVersion = engineVersion;
            IsApiServer = true;
            IsWorker = false;
            IsReadmodelBuilder = true;

            QueueConnectionString = ConfigurationManager.ConnectionStrings["ds.queue"].ConnectionString;
            LogsConnectionString = ConfigurationManager.ConnectionStrings["log"].ConnectionString;

            QueueInfoList = new Core.Jobs.QueueManager.QueueInfo[] { };

            QueueJobsPollInterval = 100; //poll each 100 milliseconds.
            QueueStreamPollInterval = 1000;
            IsQueueManager = false;
            TenantSettings.Add(new TestTenantSettings(tenantId));
            BucketInfo = new List<BucketInfo>() { new BucketInfo() { Slots = new[] { "*" }, BufferSize = 10 } };
            Boost = "true";
            DelayedStartInMilliseconds = 1000;
            ForcedGcSecondsInterval = 0;
            EngineSlots = new String[] { "*" };
            PollingMsInterval = 100;
            AllowedFileTypes = "pdf|xls|xlsx|docx|doc|ppt|pptx|pps|ppsx|rtf|odt|ods|odp|htmlzip|eml|msg|jpeg|jpg|png|zip|txt".Split('|');
            IsDeduplicationActive = true;
            StorageType = StorageType.GridFs;
        }

        public override void CreateLoggingFacility(LoggingFacility f)
        {
            f.LogUsing<ExtendedConsoleLoggerFactory>();
        }

        public void SetTestAddress(Uri serverAddress)
        {
            AddServerAddress(serverAddress.AbsoluteUri);
        }

        public void SetFolderToMonitor(String folder)
        {
            FoldersToMonitor = new String[] { folder };
        }
    }

    public class DocumentStoreTestConfigurationForPollQueue : DocumentStoreTestConfiguration
    {
        public DocumentStoreTestConfigurationForPollQueue( QueueInfo[] queueInfo, String engineVersion = "v3")
        {
            IsQueueManager = true;
            QueueJobsPollInterval = 50; //poll each 50 milliseconds.
            QueueStreamPollInterval = 50;
            EngineVersion = engineVersion;
            this.QueueInfoList = queueInfo;
        }

        public override void CreateLoggingFacility(LoggingFacility f)
        {
            f.LogUsing<ExtendedConsoleLoggerFactory>();
        }
    }
}