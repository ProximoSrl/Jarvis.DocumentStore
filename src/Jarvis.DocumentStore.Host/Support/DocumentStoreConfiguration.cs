using System;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ConfigurationService.Client;
using Jarvis.DocumentStore.Core.Support;
using System.Collections;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using System.Collections.Generic;

namespace Jarvis.DocumentStore.Host.Support
{
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
            QueueConnectionString = ConfigurationServiceClient.Instance.GetSetting("connectionStrings.ds-queues");
            IsApiServer = GetBool("api");
            IsWorker = GetBool("worker");
            IsReadmodelBuilder = GetBool("projections");
            IsQueueManager = GetBool("queueManager");

            QueueStreamPollTime = GetInt32("queues.stream-poll-interval-ms", 1000);
            List<QueueInfo> queueInfoList = new List<QueueInfo>();
            if (IsQueueManager)
            {
                FillQueueList(queueInfoList);
            }
            QueueInfoList = queueInfoList.ToArray();
        }

        private static void FillQueueList(List<QueueInfo> queueInfoList)
        {
            dynamic queueList = ConfigurationServiceClient.Instance.GetStructuredSetting("queues.list");

            foreach (dynamic queue in (IEnumerable)queueList)
            {
                QueueInfo info = new QueueInfo();
                info.Name = queue.name;
                info.Extension = queue.extension;
                info.Pipeline = queue.pipeline;
                queueInfoList.Add(info);
            }
        }

        Int32 GetInt32(string name, Int32 defaultValue)
        {
            var setting = ConfigurationServiceClient.Instance.GetSetting(name, defaultValue.ToString());
            return Int32.Parse(setting);
        }

        bool GetBool(string name)
        {
            var setting = ConfigurationServiceClient.Instance.GetSetting("roles." + name, "false");
            return "true".Equals(setting, StringComparison.OrdinalIgnoreCase);
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
                    if (exception != null)
                    {
                        Console.WriteLine("ERROR: {0}\n{1}", message, exception.Message);
                    }
                    else
                    {
                        Console.WriteLine("ERROR: {0}", message);
                    }

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
