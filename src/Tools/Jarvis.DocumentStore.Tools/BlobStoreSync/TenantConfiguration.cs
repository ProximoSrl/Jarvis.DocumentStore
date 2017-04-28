using Castle.Core.Logging;
using Jarvis.ConfigurationService.Client;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Core.Storage.FileSystem;
using Jarvis.DocumentStore.Core.Storage.GridFs;
using MongoDB.Driver;
using System;
using System.Configuration;

namespace Jarvis.DocumentStore.Shell.BlobStoreSync
{
    public class TenantConfiguration
    {
        public TenantConfiguration(
            String tenantId,
            ILogger logger)
        {
            TenantId = tenantId;

            //Eventstore connection string and username and password for file system.
            var eventStoreConnectionString = ConfigurationManager.AppSettings[$"eventStoreconnection-{tenantId}"];
            var descriptorConnectionString = ConfigurationManager.AppSettings[$"fileSystemStoreDescriptors-{tenantId}"];
            var fileSystemRootUserName = ConfigurationManager.AppSettings["fileSystemStoreUserName"];
            var fileSystemRootPassword = ConfigurationManager.AppSettings["fileSystemStorePassword"];

            EventStoreDb = GetDb(eventStoreConnectionString);

            //We need to have original blob store and destination blob store for original blob
            var originalConnectionString =  ConfigurationManager.AppSettings[$"OriginalBlobConnection-{tenantId}"];
            if (String.IsNullOrEmpty(originalConnectionString))
            {
                throw new ConfigurationErrorsException($"App settings OriginalBlobConnection-{tenantId} with connection string of original blob store not found");
            }

            OriginalGridFsBlobStore = new GridFsBlobStore(GetLegacyDb(originalConnectionString), null) { Logger = logger };
            var originalFileSystemRoot = ConfigurationManager.AppSettings[$"fileSystemStoreOriginal-{tenantId}"];

            if (!String.IsNullOrEmpty(fileSystemRootUserName))
            {
                PinvokeWindowsNetworking.ConnectToRemote(originalFileSystemRoot, fileSystemRootUserName, fileSystemRootPassword);
            }

            if (String.IsNullOrEmpty(originalFileSystemRoot))
            {
                throw new ConfigurationErrorsException($"File system settings for tenant {tenantId} (settings fileSystemStoreOriginal-{tenantId}) not found in configuration");
            }

            OriginalFileSystemBlobStore = new FileSystemBlobStore(
                GetDb(descriptorConnectionString),
                FileSystemBlobStore.OriginalDescriptorStorageCollectionName,
                originalFileSystemRoot,
                null, //Counter service is not needed for the migrator
                fileSystemRootUserName,
                fileSystemRootPassword
                )
            {
                Logger = logger
            };

            //we need to have artifacts blob store for source and destination
            //We need to have original blob store and destination blob store for original blob
            var artifactConnectionString = ConfigurationManager.AppSettings[$"ArtifactBlobConnection-{tenantId}"];
            if (String.IsNullOrEmpty(artifactConnectionString))
            {
                throw new ConfigurationErrorsException($"App settings ArtifactBlobConnection-{tenantId} with connection string of original blob store not found");
            }

            ArtifactsGridFsBlobStore = new GridFsBlobStore(GetLegacyDb(artifactConnectionString), null) { Logger = logger };
            var artifactFileSystemRoot = ConfigurationManager.AppSettings[$"fileSystemStoreArtifacts-{tenantId}"];
            if (String.IsNullOrEmpty(artifactFileSystemRoot))
            {
                throw new ConfigurationErrorsException($"File system settings for tenant {tenantId} (settings fileSystemStoreArtifacts-{tenantId}) not found in configuration");
            }

            ArtifactsFileSystemBlobStore = new FileSystemBlobStore(
                GetDb(descriptorConnectionString),
                FileSystemBlobStore.ArtifactsDescriptorStorageCollectionName,
                artifactFileSystemRoot,
                null,
                FileSystemUserName,
                FileSystemPassword)
            {
                Logger = logger
            };

            //Null counter service is used to ensure that no new blob could be created.
            ArtifactsGridFsBlobStore = new GridFsBlobStore(GetLegacyDb(artifactConnectionString), null) { Logger = logger };
        }

        public String TenantId { get; }

        public GridFsBlobStore OriginalGridFsBlobStore { get; private set; }

        public IMongoDatabase EventStoreDb { get; private set; }

        public GridFsBlobStore ArtifactsGridFsBlobStore { get; private set; }

        public FileSystemBlobStore OriginalFileSystemBlobStore { get; private set; }

        public FileSystemBlobStore ArtifactsFileSystemBlobStore { get; private set; }

        public String FileSystemUserName { get; set; }

        public String FileSystemPassword { get; set; }

        private MongoDatabase GetLegacyDb(String connectionString)
        {
            var url = new MongoUrl(connectionString);
#pragma warning disable CS0618 // Type or member is obsolete
            return new MongoClient(url).GetServer().GetDatabase(url.DatabaseName);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private IMongoDatabase GetDb(String connectionString)
        {
            var url = new MongoUrl(connectionString);
            return new MongoClient(url).GetDatabase(url.DatabaseName);
        }
    }
}
