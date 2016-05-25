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
using Jarvis.Framework.Kernel.ProjectionEngine;
using System.IO;

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

            QueueConnectionString = ConfigurationServiceClient.Instance.GetSetting("connectionStrings.ds-queues");
            LogsConnectionString = ConfigurationServiceClient.Instance.GetSetting("connectionStrings.ds-logs");

            IsApiServer = GetBool("api");
            IsWorker = GetBool("worker");
            IsReadmodelBuilder = GetBool("projections");
            IsQueueManager = GetBool("queueManager");

            JobsManagement = JsonConvert.DeserializeObject<JobsManagementConfiguration>(
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
                AddServerAddress((string)binding);
            }

            var metersOptions = ConfigurationServiceClient.Instance.GetStructuredSetting("meters");
            foreach (var binding in metersOptions)
            {
                AddMetersOptions((string)binding.Name, (string)binding.Value);
            }

            EngineVersion = ConfigurationServiceClient.Instance.GetSetting("projection-engine-version", "v1");
            ConfigurationServiceClient.Instance.WithArraySetting("poller-buckets", buckets =>
            {
                if (buckets != null)
                {
                    BucketInfo = buckets.Select(b => new BucketInfo()
                    {
                        Slots = b.slots.ToString().Split(','),
                        BufferSize = (Int32)b.buffer,
                    }).ToList();
                }
                else
                {
                    //No bucket configured, just start with a single bucket for all slots.
                    BucketInfo = new List<BucketInfo>()
                {
                    new BucketInfo() {BufferSize = 5000, Slots = new [] { "*" } }
                };
                }
            });
            Rebuild = "true".Equals(ConfigurationServiceClient.Instance.GetSetting("rebuild", "false"), StringComparison.OrdinalIgnoreCase);
            NitroMode = "true".Equals(ConfigurationServiceClient.Instance.GetSetting("nitro-mode", "false"), StringComparison.OrdinalIgnoreCase);
            EngineSlots = ConfigurationServiceClient.Instance.GetSetting("engine-slots", "*").Split(',');
        
            PollingMsInterval = Int32.Parse(ConfigurationServiceClient.Instance.GetSetting("polling-interval-ms", "1000"));
            ForcedGcSecondsInterval = Int32.Parse(ConfigurationServiceClient.Instance.GetSetting("memory-collect-seconds", "0"));
            DelayedStartInMilliseconds = Int32.Parse(ConfigurationServiceClient.Instance.GetSetting("poller-delayed-start", "2000"));

            Boost = ConfigurationServiceClient.Instance.GetSetting("engine-multithread", "false");

            // import from filesystem
            var fileQueue = ConfigurationServiceClient.Instance.GetStructuredSetting("file-queue");
            var listOfFolders = new List<string>();
            foreach (var folder in fileQueue)
            {
                listOfFolders.Add((string) folder);
            }
            MonitorFolders(listOfFolders.ToArray());
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
            //this is the configuration with the base parameters value.
            //var defaultParameterFile = new FileInfo("default-parameters.config");
            //ConfigurationServiceClient.AppDomainInitializer(
            //    LoggerFunction, 
            //    "JARVIS_CONFIG_SERVICE",
            //    defaultParameterFile : defaultParameterFile);
            var defaultParameterFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "defaultParameters.config");
            ConfigurationServiceClient.AppDomainInitializer(
                    LoggerFunction,
                    "JARVIS_CONFIG_SERVICE",
                    defaultConfigFile : new FileInfo(defaultParameterFileName)
                );
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
