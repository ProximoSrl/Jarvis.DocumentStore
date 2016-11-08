using Jarvis.ConfigurationService.Client;
using Jarvis.Framework.Kernel.MultitenantSupport;
using Jarvis.Framework.Shared.MultitenantSupport;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Host.Support
{
    public class DocumentStoreTenantSettings : TenantSettings
    {
        private void SetConnectionString(string name)
        {
            Set(
                "connectionstring." + name,
                ConfigurationServiceClient.Instance.GetSetting("connectionStrings." + TenantId + "." + name)
            );
        }

        public DocumentStoreTenantSettings(string tenantId)
            : base(new TenantId(tenantId))
        {
            SetConnectionString("events");
            SetConnectionString("originals");
            SetConnectionString("artifacts");
            SetConnectionString("system");
            SetConnectionString("readmodel");

            Set("system.db", GetDatabase("system"));
            Set("originals.db", GetDatabase("originals"));
            Set("artifacts.db", GetDatabase("artifacts"));
            Set("readmodel.db", GetDatabase("readmodel"));

            Set("originals.db.legacy", GetLegacyDatabase("originals"));
            Set("artifacts.db.legacy", GetLegacyDatabase("artifacts"));
        }

        private MongoDatabase GetLegacyDatabase(string connectionStringName)
        {
            MongoUrl url = new MongoUrl(this.GetConnectionString(connectionStringName));
            MongoClient client = new MongoClient(url);
            return client.GetServer().GetDatabase(url.DatabaseName);
        }
    }
}