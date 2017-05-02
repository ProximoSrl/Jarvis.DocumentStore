using System.Configuration;
using Jarvis.Framework.Kernel.MultitenantSupport;
using Jarvis.Framework.Shared.MultitenantSupport;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Tests.Support
{
    public class TestTenantSettings : TenantSettings
    {
        public TestTenantSettings(string tenantId) : base(new TenantId(tenantId))
        {
            SetConnectionString("events");
            SetConnectionString("originals");
            SetConnectionString("artifacts");
            SetConnectionString("system");
            SetConnectionString("readmodel");

            Set("originals.db", GetDatabase("originals"));
            Set("artifacts.db", GetDatabase("artifacts"));

            Set("system.db", GetDatabase("system"));
            Set("readmodel.db", GetDatabase("readmodel"));
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