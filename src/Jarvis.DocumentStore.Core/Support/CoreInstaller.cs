using System.Configuration;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using CQRS.Kernel.Commands;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Shared.Commands;
using CQRS.Shared.Factories;
using Jarvis.DocumentStore.Core.Services;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Core.Support
{
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
