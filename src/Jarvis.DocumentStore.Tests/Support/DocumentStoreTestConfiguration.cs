using System.Configuration;
using Castle.Facilities.Logging;
using Castle.Services.Logging.Log4netIntegration;
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

            TenantSettings.Add(new TestTenantSettings());
        }

        public override void CreateLoggingFacility(LoggingFacility f)
        {
            f.LogUsing<ExtendedConsoleLoggerFactory>();
        }
    }
}