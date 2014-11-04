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
            SetConnectionString("originals");
            SetConnectionString("artifacts");
            SetConnectionString("system");
            SetConnectionString("readmodel");

            Set("originals.db", GetDatabase("originals"));
            Set("artifacts.db", GetDatabase("artifacts"));

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