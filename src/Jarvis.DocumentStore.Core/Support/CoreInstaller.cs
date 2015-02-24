using System.Configuration;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.Framework.Kernel.Commands;
using Jarvis.Framework.Shared.Commands;
using MongoDB.Driver;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.Framework.Shared.IoC;

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

            container.Register(
                Component
                    .For<IDocumentFormatTranslator>()
                    .ImplementedBy<StandardDocumentFormatTranslator>(),
                Component
                    .For<Document>()
                    .LifestyleCustom(typeof(TransientNotTrackingLifestyle))
            );
        }
    }
}
