using Jarvis.ConfigurationService.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.LiveBackup.Support
{
    public abstract class Configuration
    {

        public Configuration()
        {
            TenantSettingList = new List<TenantSettings>();
        }

        protected void SetBackupDirectory(String value)
        {
            EnsureDirectory(value);

            EventStoreBackupDirectory = Path.Combine(value, "EventStore");
            BlobsBackupDirectory = Path.Combine(value, "Blobs");

            EnsureDirectory(EventStoreBackupDirectory);
            EnsureDirectory(BlobsBackupDirectory);
        }

        private static void EnsureDirectory(string value)
        {
            if (!Directory.Exists(value))
                Directory.CreateDirectory(value);
        }

        public String EventStoreBackupDirectory { get; private set; }

        public String BlobsBackupDirectory { get; private set; }

        public IList<TenantSettings> TenantSettingList { get; private set; }

        public Int64 MaxFileSize { get; protected set; }

        public Boolean CompressionEnabled { get; protected set; }

        public class TenantSettings
        {
            public TenantSettings(string tenantId)
            {
                TenantId = tenantId;
                EventStoreConnectionString = ConfigurationServiceClient.Instance.GetSetting("connectionStrings." + tenantId + ".events");
                OriginalBlobConnnectionString = ConfigurationServiceClient.Instance.GetSetting("connectionStrings." + tenantId + ".originals");
            }

            public String TenantId { get; private set; }

            public String EventStoreConnectionString { get; private set; }

            public String OriginalBlobConnnectionString { get; private set; }

            internal String GetEventsDumpFileName(string eventStoreBackupDirectory, String databaseName)
            {
                var directory = Path.Combine(eventStoreBackupDirectory, TenantId);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                return Path.Combine(directory, databaseName + ".dump");
            }
        }
    }

 
    public class ConfigurationServiceSettingsConfiguration : Configuration
    {
        public ConfigurationServiceSettingsConfiguration()
        {
            SetBackupDirectory(ConfigurationManager.AppSettings["BackupDirectory"]);
            var tenants = ConfigurationServiceClient.Instance.GetStructuredSetting("tenants");
            foreach (string tenantId in tenants) // conversion from dynamic array
            {
                Console.WriteLine("Looking for configuration for tenant {0}", tenantId);

                this.TenantSettingList.Add( new Configuration.TenantSettings(tenantId));

            }
            MaxFileSize = 1024 * 1024 * Int32.Parse(ConfigurationManager.AppSettings["MaxFileSizeInMB"]);
            CompressionEnabled = "true".Equals(ConfigurationManager.AppSettings["CompressionEnabled"], StringComparison.OrdinalIgnoreCase);
        }

    }
}
