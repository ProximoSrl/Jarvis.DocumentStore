using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.Framework.Kernel.Commands;
using Jarvis.Framework.Shared.Commands;
using Jarvis.Framework.Shared.IoC;
using Jarvis.Framework.Shared.ReadModel;
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
            var logUrl = new MongoUrl(_config.LogsConnectionString);
            var logDb = new MongoClient(logUrl).GetServer().GetDatabase(logUrl.DatabaseName);
          
               
            container.Register(
                 Component
                    .For<IMessagesTracker>()
                    .ImplementedBy<MongoDbMessagesTracker>()
                    .DependsOn(Dependency.OnValue<MongoDatabase>(logDb)),
                Component
                    .For<ICommandBus, IInProcessCommandBus>()
                    .ImplementedBy<MultiTenantInProcessCommandBus>()
                );

            container.Register(
                Component
                    .For<IDocumentFormatTranslator>()
                    .ImplementedBy<StandardDocumentFormatTranslator>(),
                Component
                    .For<DocumentDescriptor>()
                    .LifestyleCustom(typeof(TransientNotTrackingLifestyle))
            );
        }
    }
}
