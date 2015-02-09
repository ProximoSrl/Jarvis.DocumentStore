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
            ServerAddress = new Uri(ConfigurationManager.AppSettings["endPoint"]);
        }

        private static void FillQueueList(List<QueueInfo> queueInfoList)
        {
            dynamic queueList = ConfigurationServiceClient.Instance.GetStructuredSetting("queues.list");

            foreach (dynamic queue in (IEnumerable)queueList)
            {
                QueueInfo info = new QueueInfo(
                    (String) queue.name,
                    (String) queue.pipeline,
                    (String) queue.extension);
                if (queue.maxNumberOfFailure != null) 
                {
                    info.MaxNumberOfFailure = (Int32)queue.maxNumberOfFailure;
                }
                if (queue.jobLockTimeout != null)
                {
                    info.JobLockTimeout = (Int32)queue.jobLockTimeout;
                }
                if (queue.parameters != null)
                {
                    info.Parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(queue.parameters.ToString());
                }
                else
                {
                    info.Parameters = new Dictionary<string, string>();
                }
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
