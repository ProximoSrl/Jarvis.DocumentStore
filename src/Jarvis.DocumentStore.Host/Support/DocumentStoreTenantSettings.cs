using CQRS.Kernel.MultitenantSupport;
using CQRS.Shared.MultitenantSupport;
using Jarvis.ConfigurationService.Client;
using MongoDB.Driver.GridFS;

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

            Set("originals.db", GetDatabase("originals"));
            Set("artifacts.db", GetDatabase("artifacts"));
            Set("db.readmodel", GetDatabase("readmodel"));
        }
    }
}