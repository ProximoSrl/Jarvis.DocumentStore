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

            Set("originals.fs", GetDatabase("originals").GetGridFS(MongoGridFSSettings.Defaults));
            Set("artifacts.fs", GetDatabase("artifacts").GetGridFS(MongoGridFSSettings.Defaults));
            Set("db.readmodel", GetDatabase("readmodel"));
        }
    }
}