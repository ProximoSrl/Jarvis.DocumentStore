using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.Storage;
using MongoDB.Driver;

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
                    .DependsOn(Dependency.OnComponent(typeof(MongoDatabase), "originals.db")),
                Component
                    .For<IBlobStore>()
                    .ImplementedBy<GridFsBlobStore>()
                    .Named("artifacts.filestore")
                    .DependsOn(Dependency.OnComponent(typeof(MongoDatabase), "artifacts.db")),
                Component
                    .For<MongoDatabase>()
                    .Named("originals.db")
                    .UsingFactoryMethod(k => _tenant.Get<MongoDatabase>("originals.db")),
                Component
                    .For<MongoDatabase>()
                    .Named("artifacts.db")
                    .UsingFactoryMethod(k => _tenant.Get<MongoDatabase>("artifacts.db"))
                );
        }
    }
}