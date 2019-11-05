using Castle.Core.Logging;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Core.Storage.GridFs;
using Jarvis.Framework.Kernel.MultitenantSupport;
using Jarvis.Framework.Shared.Logging;
using Jarvis.Framework.Shared.MultitenantSupport;
using MongoDB.Driver;
using NEventStore.Logging;
using System;
using System.Linq;

namespace Jarvis.DocumentStore.Core.Support
{
    public class TenantCoreInstaller : IWindsorInstaller
    {
        private readonly ITenant _tenant;
        private readonly DocumentStoreConfiguration _config;
        private readonly TenantSettings _tenantSettings;

        public TenantCoreInstaller(ITenant tenant, DocumentStoreConfiguration config)
        {
            _tenant = tenant;
            _config = config;
            _tenantSettings = config.TenantSettings.Single(t => t.TenantId == tenant.Id);
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            ILogger baseLogger = container.Resolve<ILogger>();
            var log = new NEventStoreLog4NetLogger(baseLogger);

            //Important, register BlobStoreByFormat as first IBlobStore interface because it is the default.
            container.Register(
                Component
                    .For<IBlobStore>()
                    .ImplementedBy<BlobStoreByFormat>()
                    .DependsOn(
                        Dependency.OnComponent("originals", StorageConstants.OriginalBlobStorageComponentName),
                        Dependency.OnComponent("artifacts", StorageConstants.ArtifactsBlobStorageComponentName)
                    ),
                 Component
                    .For<ILog>()
                    .Instance(log)
                );

            switch (_config.StorageType)
            {
                case StorageType.GridFs:
                    RegisterGridFs(container);
                    break;
                case StorageType.FileSystem:
                    RegisterFileSystemFsFs(container);
                    break;
            }
        }

        private void RegisterGridFs(IWindsorContainer container)
        {
            container.Register(
                  Component
                    .For<IBlobStore>()
                    .ImplementedBy<GridFsBlobStore>()
                    .Named(StorageConstants.OriginalBlobStorageComponentName)
                    .DependsOn(Dependency.OnComponent(typeof(MongoDatabase), "originals.db.legacy")),
                Component
                    .For<IBlobStore>()
                    .ImplementedBy<GridFsBlobStore>()
                    .Named(StorageConstants.ArtifactsBlobStorageComponentName)
                    .DependsOn(Dependency.OnComponent(typeof(MongoDatabase), "artifacts.db.legacy")),
                Component
                    .For<IMongoDatabase>()
                    .Named("originals.db")
                    .UsingFactoryMethod(_ => _tenant.Get<IMongoDatabase>("originals.db")),
                Component
                    .For<IMongoDatabase>()
                    .Named("artifacts.db")
                    .UsingFactoryMethod(_ => _tenant.Get<IMongoDatabase>("artifacts.db")),
                 Component
                    .For<MongoDatabase>()
                    .Named("originals.db.legacy")
                    .UsingFactoryMethod(_ => _tenant.Get<MongoDatabase>("originals.db.legacy")),
                Component
                    .For<MongoDatabase>()
                    .Named("artifacts.db.legacy")
                    .UsingFactoryMethod(_ => _tenant.Get<MongoDatabase>("artifacts.db.legacy"))
             );
        }

        private void RegisterFileSystemFsFs(IWindsorContainer container)
        {
            container.Register(
                 Component
                    .For<IMongoDatabase>()
                    .Named("fsdescriptor.db")
                    .UsingFactoryMethod(_ => _tenant.Get<IMongoDatabase>("fsdescriptor.db")),
                  Component
                    .For<IBlobStore>()
                    .ImplementedBy<FileSystemBlobStore>()
                    .Named(StorageConstants.OriginalBlobStorageComponentName)
                    .DependsOn(Dependency.OnComponent(typeof(IMongoDatabase), "fsdescriptor.db"))
                    .DependsOn(Dependency.OnConfigValue("collectionName", FileSystemBlobStore.OriginalDescriptorStorageCollectionName))
                    .DependsOn(Dependency.OnValue("baseDirectory", _tenantSettings.Get<String>("storage.fs.originals"))),
                Component
                    .For<IBlobStore>()
                    .ImplementedBy<FileSystemBlobStore>()
                    .Named(StorageConstants.ArtifactsBlobStorageComponentName)
                    .DependsOn(Dependency.OnComponent(typeof(IMongoDatabase), "fsdescriptor.db"))
                    .DependsOn(Dependency.OnConfigValue("collectionName", FileSystemBlobStore.ArtifactsDescriptorStorageCollectionName))
                    .DependsOn(Dependency.OnValue("baseDirectory", _tenantSettings.Get<String>("storage.fs.artifacts")))
             );
        }
    }
}