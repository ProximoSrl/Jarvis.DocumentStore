using Castle.Core.Logging;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.Framework.Shared.Logging;
using Jarvis.Framework.Shared.MultitenantSupport;
using MongoDB.Driver;
using NEventStore.Logging;

namespace Jarvis.DocumentStore.Core.Support
{
    public class TenantCoreInstaller : IWindsorInstaller
    {
        readonly ITenant _tenant;

        public TenantCoreInstaller(ITenant tenant)
        {
            _tenant = tenant;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            ILogger baseLogger = container.Resolve<ILogger>();
            var log = new NEventStoreLog4NetLogger(baseLogger);

            container.Register(
                Component
                    .For<IBlobStore>()
                    .ImplementedBy<BlobStoreByFormat>()
                    .DependsOn(
                        Dependency.OnComponent("originals", "originals.filestore"),
                        Dependency.OnComponent("artifacts", "artifacts.filestore")
                    ),
                Component
                    .For<IBlobStore>()
                    .ImplementedBy<GridFsBlobStore>()
                    .Named("originals.filestore")
                    .DependsOn(Dependency.OnComponent(typeof(MongoDatabase), "originals.db.legacy")),
                Component
                    .For<IBlobStore>()
                    .ImplementedBy<GridFsBlobStore>()
                    .Named("artifacts.filestore")
                    .DependsOn(Dependency.OnComponent(typeof(MongoDatabase), "artifacts.db.legacy")),
                Component
                    .For<IMongoDatabase>()
                    .Named("originals.db")
                    .UsingFactoryMethod(k => _tenant.Get<IMongoDatabase>("originals.db")),
                Component
                    .For<IMongoDatabase>()
                    .Named("artifacts.db")
                    .UsingFactoryMethod(k => _tenant.Get<IMongoDatabase>("artifacts.db")),
                 Component
                    .For<MongoDatabase>()
                    .Named("originals.db.legacy")
                    .UsingFactoryMethod(k => _tenant.Get<MongoDatabase>("originals.db.legacy")),
                Component
                    .For<MongoDatabase>()
                    .Named("artifacts.db.legacy")
                    .UsingFactoryMethod(k => _tenant.Get<MongoDatabase>("artifacts.db.legacy")),
                 Component
                    .For<ILog>()
                    .Instance(log)
                );
        }
    }
}