using Castle.Core.Logging;
using Jarvis.ConfigurationService.Client;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Core.Storage.GridFs;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.Framework.CommitBackup.Core;
using MongoDB.Driver;
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
        protected Configuration()
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

        public StorageType StorageType { get; set; }

        public String EventStoreBackupDirectory { get; private set; }

        public String BlobsBackupDirectory { get; private set; }

        public IList<TenantSettings> TenantSettingList { get; private set; }

        public Int64 MaxFileSize { get; protected set; }

        public Boolean CompressionEnabled { get; protected set; }

        internal TenantCommitBackupperConfiguration GetBackupperConfiguration(String tenantId)
        {
            var tenantConfiguration = TenantSettingList.Single(t => t.TenantId.Equals(tenantId, StringComparison.OrdinalIgnoreCase));
            return new TenantCommitBackupperConfiguration(EventStoreBackupDirectory, tenantConfiguration.TenantId, tenantConfiguration.EventStoreConnectionString);
        }

        internal ICommitWriterFactory GetWriterFactory()
        {
            return new PlainTextFileCommitWriterFactory(MaxFileSize);
        }

        internal ICommitReaderFactory GetReaderFactory()
        {
            return new PlainMongoDbCommitReaderFactory();
        }

        public class TenantSettings
        {
            private readonly StorageType _storageType;

            public TenantSettings(string tenantId, StorageType storageType, String baseBackupDirectory)
            {
                TenantId = tenantId;
                EventStoreConnectionString = ConfigurationServiceClient.Instance.GetSetting("connectionStrings." + tenantId + ".events");
                _storageType = storageType;
            }

            public String TenantId { get; private set; }

            public String EventStoreConnectionString { get; private set; }

            public IBlobStore GetBlobStore(ILogger logger)
            {
                EventStoreConnectionString = ConfigurationServiceClient.Instance.GetSetting("connectionStrings." + TenantId + ".originals");
                switch (_storageType)
                {
                    case StorageType.GridFs:
                        return new GridFsBlobStore(GetLegacyDb(EventStoreConnectionString), null) { Logger = logger };

                    case StorageType.FileSystem:
                        var originalFileSystemStorage = ConfigurationServiceClient.Instance.GetSetting($"storage.fileSystem.{TenantId}-originals-baseDirectory");
                        var fileSystemUserName = ConfigurationServiceClient.Instance.GetSetting($"storage.fileSystem.username", "");
                        var fileSystemPassword = ConfigurationServiceClient.Instance.GetSetting($"storage.fileSystem.password", "");
                        return new FileSystemBlobStore(
                            GetDb(EventStoreConnectionString),
                            FileSystemBlobStore.OriginalDescriptorStorageCollectionName,
                            originalFileSystemStorage,
                            null,
                            fileSystemUserName,
                            fileSystemPassword)
                        { Logger = logger };

                    default:
                        throw new NotImplementedException("Storage type not implemented");
                }
            }

            internal String GetEventsDumpFileName(string eventStoreBackupDirectory, String databaseName)
            {
                var directory = Path.Combine(eventStoreBackupDirectory, TenantId);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                return Path.Combine(directory, databaseName + ".dump");
            }

            private IMongoDatabase GetDb(String connectionString)
            {
                var url = new MongoUrl(connectionString);
                return new MongoClient(url).GetDatabase(url.DatabaseName);
            }

            private MongoDatabase GetLegacyDb(String connectionString)
            {
                var url = new MongoUrl(connectionString);
#pragma warning disable CS0618 // Type or member is obsolete
                return new MongoClient(url).GetServer().GetDatabase(url.DatabaseName);
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }
    }

    public class ConfigurationServiceSettingsConfiguration : Configuration
    {
        public ConfigurationServiceSettingsConfiguration()
        {
            var storageTypeString = ConfigurationServiceClient.Instance.GetSetting("storageType", "GridFs");
            storageTypeString = String.IsNullOrEmpty(storageTypeString) ? "GridFs" : storageTypeString;
            StorageType storageType;
            if (!Enum.TryParse<StorageType>(storageTypeString, true, out storageType))
            {
                throw new ConfigurationErrorsException($"Mandatory settings Storage.Type not found.");
            }
            this.StorageType = storageType;

            SetBackupDirectory(ConfigurationManager.AppSettings["BackupDirectory"]);
            var tenants = ConfigurationServiceClient.Instance.GetStructuredSetting("tenants");
            foreach (string tenantId in tenants) // conversion from dynamic array
            {
                Console.WriteLine("Looking for configuration for tenant {0}", tenantId);

                this.TenantSettingList.Add(new Configuration.TenantSettings(tenantId, StorageType, EventStoreBackupDirectory));
            }
            MaxFileSize = 1024 * 1024 * Int32.Parse(ConfigurationManager.AppSettings["MaxFileSizeInMB"]);
            CompressionEnabled = "true".Equals(ConfigurationManager.AppSettings["CompressionEnabled"], StringComparison.OrdinalIgnoreCase);
        }
    }

    public class TenantCommitBackupperConfiguration : CommitBackupperConfiguration
    {
        public TenantCommitBackupperConfiguration(
            String baseBackupDirectory,
            String tenantId,
            String databaseConnectionString)
        {
            BackupDirectory = Path.Combine(baseBackupDirectory, tenantId);
            MaxFileSize = 50000;
            DatabasesToBackup.Add(databaseConnectionString);
        }
    }
}
