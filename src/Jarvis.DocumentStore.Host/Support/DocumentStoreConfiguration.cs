using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Kernel.MultitenantSupport;
using Jarvis.ConfigurationService.Client;

namespace Jarvis.DocumentStore.Host.Support
{
    public abstract class DocumentStoreConfiguration
    {
        protected DocumentStoreConfiguration()
        {
            TenantSettings = new List<TenantSettings>();
        }

        public bool IsApiServer { get; protected set; }
        public bool IsWorker { get; protected set; }
        public bool IsReadmodelBuilder { get; protected set; }
        public string QuartzConnectionString { get; protected set; }

        public IList<TenantSettings> TenantSettings { get; private set; }
    }

    public class RemoteDocumentStoreConfiguration : DocumentStoreConfiguration
    {
        public RemoteDocumentStoreConfiguration()
        {
            BootstrapConfigurationServiceClient();
            Configure();
        }

        void Configure()
        {
            var tenants = ConfigurationServiceClient.Instance.GetStructuredSetting("tenants");
            foreach (string tenant in tenants) // conversion from dynamic array
            {
                TenantSettings.Add(new DocumentStoreTenantSettings(tenant));
            }

            QuartzConnectionString = ConfigurationServiceClient.Instance.GetSetting("connectionStrings.ds-quartz");
            IsApiServer = GetBool("api");
            IsWorker = GetBool("worker");
            IsReadmodelBuilder = GetBool("projections");
        }

        bool GetBool(string name)
        {
            var setting = ConfigurationServiceClient.Instance.GetSetting("roles."+name, "false").ToLowerInvariant();
            return  setting == "true";
        }

        private void BootstrapConfigurationServiceClient()
        {
            ConfigurationServiceClient.AppDomainInitializer(LoggerFunction, "JARVIS_CONFIG_SERVICE");
        }

        private void LoggerFunction(string message, bool isError, Exception exception)
        {
            if (Environment.UserInteractive)
            {
                if (isError)
                {
                    Console.WriteLine("ERROR: {0}\n{1}", message, exception.Message);
                    Console.WriteLine("Press enter to continue");
                    Console.ReadLine();
                }
                else
                {
                    Console.WriteLine("INFO : {0}", message);
                }
            }
        }
    }
}
