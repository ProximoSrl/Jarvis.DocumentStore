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

            QuartzConnectionString = ConfigurationManager.ConnectionStrings["ds.quartz"].ConnectionString;
            QueueConnectionString = ConfigurationManager.ConnectionStrings["ds.queue"].ConnectionString;
            QueueInfoList = new Core.Jobs.QueueManager.QueueInfo[] { };

            QueueJobsPollInterval = 100; //poll each 100 milliseconds.
            QueueStreamPollInterval = 1000;
            IsQueueManager = false;
            TenantSettings.Add(new TestTenantSettings());
        }

        public override void CreateLoggingFacility(LoggingFacility f)
        {
            f.LogUsing<ExtendedConsoleLoggerFactory>();
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