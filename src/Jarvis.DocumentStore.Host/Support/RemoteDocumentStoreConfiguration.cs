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
using Newtonsoft.Json;
using System.Threading;

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

            IsDeduplicationActive = "true".Equals(ConfigurationServiceClient.Instance.GetSetting("deduplication-active", "true"), StringComparison.OrdinalIgnoreCase);
            var allowedFileList = ConfigurationServiceClient.Instance.GetSetting("allowed-file-types", "pdf|xls|xlsx|docx|doc|ppt|pptx|pps|ppsx|rtf|odt|ods|odp|htmlzip|eml|msg|jpeg|jpg|png|zip");

            AllowedFileTypes = allowedFileList != "*" ? allowedFileList.Split('|') : null;

            QuartzConnectionString = ConfigurationServiceClient.Instance.GetSetting("connectionStrings.ds-quartz");
            QueueConnectionString = ConfigurationServiceClient.Instance.GetSetting("connectionStrings.ds-queues");
            IsApiServer = GetBool("api");
            IsWorker = GetBool("worker");
            IsReadmodelBuilder = GetBool("projections");
            IsQueueManager = GetBool("queueManager");

            var jobMode = ConfigurationServiceClient.Instance.GetSetting("roles.jobMode", "Quartz");
            JobMode = (JobModes) Enum.Parse(typeof(JobModes), jobMode, true);
            this.JobsManagement = JsonConvert.DeserializeObject<JobsManagementConfiguration>(
                    ConfigurationServiceClient.Instance.GetSetting("jobsManagement"));
            QueueStreamPollInterval = GetInt32("queues.stream-poll-interval-ms", 1000);
            QueueJobsPollInterval = GetInt32("queues.jobs-poll-interval-ms", 1000);
            List<QueueInfo> queueInfoList = new List<QueueInfo>();
            if (IsQueueManager)
            {
                FillQueueList(queueInfoList);
            }
            QueueInfoList = queueInfoList.ToArray();

            //App.config configuration
            var apiBindings = ConfigurationServiceClient.Instance.GetStructuredSetting("api-bindings");
            foreach (var binding in apiBindings)
            {
                AddServerAddress(new Uri((string)binding));
            }

            var metersOptions = ConfigurationServiceClient.Instance.GetStructuredSetting("meters");
            foreach (var binding in metersOptions)
            {
                AddMetersOptions((string)binding.Name, (string)binding.Value);
            }

            Rebuild = "true".Equals(ConfigurationServiceClient.Instance.GetSetting("rebuild", "false"), StringComparison.OrdinalIgnoreCase);
            NitroMode = "true".Equals(ConfigurationServiceClient.Instance.GetSetting("nitro-mode", "false"), StringComparison.OrdinalIgnoreCase);
            EngineSlots = ConfigurationServiceClient.Instance.GetSetting("engine-slots", "*").Split(',');
        
            PollingMsInterval = Int32.Parse(ConfigurationServiceClient.Instance.GetSetting("polling-interval-ms", "1000"));
            ForcedGcSecondsInterval = Int32.Parse(ConfigurationServiceClient.Instance.GetSetting("memory-collect-seconds", "0"));
            DelayedStartInMilliseconds = Int32.Parse(ConfigurationServiceClient.Instance.GetSetting("poller-delayed-start", "2000"));

            Boost = ConfigurationServiceClient.Instance.GetSetting("engine-multithread", "false");
        }

        private static void FillQueueList(List<QueueInfo> queueInfoList)
        {
            dynamic queueList = ConfigurationServiceClient.Instance.GetStructuredSetting("queues.list");

            ParseQueueList(queueInfoList, queueList);
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
