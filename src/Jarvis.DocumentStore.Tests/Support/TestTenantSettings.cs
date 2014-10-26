using System.Configuration;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Shared.MultitenantSupport;
using MongoDB.Driver.GridFS;

namespace Jarvis.DocumentStore.Tests.Support
{
    public class TestTenantSettings :TenantSettings{
        public TestTenantSettings(): base(new TenantId("tests"))
        {
            SetConnectionString("events");
            SetConnectionString("filestore");
            SetConnectionString("system");
            SetConnectionString("readmodel");

            Set("grid.fs",GetDatabase("filestore").GetGridFS(MongoGridFSSettings.Defaults));
            Set("db.readmodel",GetDatabase("readmodel"));
        }

        private void SetConnectionString(string name)
        {
            Set(
                "connectionstring." + name,
                ConfigurationManager.ConnectionStrings[TenantId + "." + name].ConnectionString
                );
        }
    }
}