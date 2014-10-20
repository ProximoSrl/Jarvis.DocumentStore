using System.Configuration;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Shared.MultitenantSupport;
using MongoDB.Driver.GridFS;

namespace Jarvis.DocumentStore.Host.Support
{
    public class DocumentStoreTenantSettings : TenantSettings
    {
        public DocumentStoreTenantSettings(string tenantId) : base(new TenantId(tenantId))
        {
            Set(
                "connectionstring.events",
                ConfigurationManager.ConnectionStrings[tenantId + ".events"].ConnectionString
                );

            Set(
                "connectionstring.files",
                ConfigurationManager.ConnectionStrings[tenantId + ".filestore"].ConnectionString
                );

            Set(
                "connectionstring.system",
                ConfigurationManager.ConnectionStrings[tenantId + ".system"].ConnectionString
                );

            Set(
                "connectionstring.readmodel",
                ConfigurationManager.ConnectionStrings[tenantId + ".readmodel"].ConnectionString
                );

            Set(
                "grid.fs",
                GetDatabase("files").GetGridFS(MongoGridFSSettings.Defaults)
            );

            Set(
                "db.readmodel",
                GetDatabase("readmodel")
            );
        }
    }
}