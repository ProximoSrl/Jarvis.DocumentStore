using System.Configuration;
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
    }
}