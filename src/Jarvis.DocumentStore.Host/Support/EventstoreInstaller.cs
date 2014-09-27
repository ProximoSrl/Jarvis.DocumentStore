using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using CommonDomain;
using CommonDomain.Core;
using CQRS.Kernel.Engine;
using CQRS.Kernel.Engine.Snapshots;
using CQRS.Kernel.Store;
using CQRS.Shared.IdentitySupport;
using CQRS.Shared.Storage;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NEventStore;
using NEventStore.Dispatcher;

namespace Jarvis.DocumentStore.Host.Support
{
    public class EventStoreInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            var sysUrl = new MongoUrl(ConfigurationManager.ConnectionStrings["system"].ConnectionString);
            var sysdb = new MongoClient(sysUrl).GetServer().GetDatabase(sysUrl.DatabaseName);

            container.Register(
                Component
                    .For<EventStoreFactory>()
                    .DependsOn(Dependency.OnValue("connectionString", "events"))
                    .LifestyleSingleton(),
                Component
                    .For<IStoreEvents>()
                    .Named("eventStore")
                    .UsingFactory<EventStoreFactory, IStoreEvents>(f => f.BuildEventStore())
                    .LifestyleSingleton(),
                Component
                    .For<ICQRSConstructAggregates>()
                    .ImplementedBy<AggregateFactory>(),
                Component
                    .For<IDetectConflicts>()
                    .ImplementedBy<ConflictDetector>()
                    .LifestyleTransient(),
                Component
                    .For<IDispatchCommits>()
                    .ImplementedBy<NullDispatcher>()
                    .LifestyleSingleton(),
                Component
                    .For<IIdentityManager, IIdentityGenerator, IIdentityConverter, IdentityManager>()
                    .ImplementedBy<IdentityManager>(),
                Component
                    .For<ICounterService>()
                    .ImplementedBy<CounterService>()
                    .DependsOn(Dependency.OnValue<MongoDatabase>(sysdb)),
                Component
                    .For<ICQRSRepository>()
                    .ImplementedBy<CQRSRepository>()
                    .DependsOn(Dependency.OnComponent(typeof(IStoreEvents), "eventStore"))
                    .LifestyleTransient(),
                Classes
                    .FromAssemblyContaining<Document>()
                    .BasedOn<AggregateBase>()
                    .WithService.Self()
                    .LifestyleTransient()
                );

            var converter = container.Resolve<IdentityManager>();

            MessagesRegistration.RegisterAssembly(typeof(Document).Assembly);
            MessagesRegistration.RegisterAssembly(typeof(DocumentCreated).Assembly);
            SnapshotRegistration.AutomapAggregateState(typeof(DocumentState).Assembly);

            converter.RegisterIdentitiesFromAssembly(typeof(DocumentId).Assembly);
            IdentitiesRegistration.RegisterFromAssembly(typeof(DocumentId).Assembly);
        }
    }
}
