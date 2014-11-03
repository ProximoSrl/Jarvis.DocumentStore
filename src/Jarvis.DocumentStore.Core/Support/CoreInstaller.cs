using System.Configuration;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using CQRS.Kernel.Commands;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Shared.Commands;
using CQRS.Shared.Factories;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using Jarvis.DocumentStore.Core.Processing.Pipeline;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Core.Storage.Stats;
using MongoDB.Driver;
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
                    .For<IFileStore>()
                    .ImplementedBy<GridFSFileStore>(),
                Component
                    .For<IPipelineManager>()
                    .ImplementedBy<PipelineManager>(),
                Component
                    .For<GridFsFileStoreStats>(),
                Component
                    .For<MongoGridFS>()
                    .UsingFactoryMethod(k => _tenant.Get<MongoGridFS>("grid.fs"))
            );
        }
    }

    public class CoreInstaller : IWindsorInstaller
    {
        private DocumentStoreConfiguration _config;

        public CoreInstaller(DocumentStoreConfiguration config)
        {
            _config = config;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component
                    .For<ICommandBus, IInProcessCommandBus>()
                    .ImplementedBy<MultiTenantInProcessCommandBus>()
                );

            container.Register(
                Component
                    .For<ConfigService>()
            );
        }
    }
}
