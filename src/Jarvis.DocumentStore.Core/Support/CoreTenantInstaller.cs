using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.Processing.Pipeline;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Core.Storage.Stats;
using MongoDB.Driver.GridFS;

namespace Jarvis.DocumentStore.Core.Support
{
    public class CoreTenantInstaller : IWindsorInstaller
    {
        readonly ITenant _tenant;

        public CoreTenantInstaller(ITenant tenant)
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
                    .DependsOn(Dependency.OnComponent(typeof(MongoGridFS), "originals.gridfs")),
                Component
                    .For<IBlobStore>()
                    .ImplementedBy<GridFsBlobStore>()
                    .Named("artifacts.filestore")
                    .DependsOn(Dependency.OnComponent(typeof(MongoGridFS), "artifacts.gridfs")),
                Component
                    .For<IPipelineManager>()
                    .ImplementedBy<PipelineManager>(),
                Component
                    .For<GridFsFileStoreStats>(),
                Component
                    .For<MongoGridFS>()
                    .Named("originals.gridfs")
                    .UsingFactoryMethod(k => _tenant.Get<MongoGridFS>("originals.fs")),
                Component
                    .For<MongoGridFS>()
                    .Named("artifacts.gridfs")
                    .UsingFactoryMethod(k => _tenant.Get<MongoGridFS>("artifacts.fs"))
                );
        }
    }
}