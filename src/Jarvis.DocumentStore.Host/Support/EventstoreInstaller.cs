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
using CQRS.Shared.Domain.Serialization;
using CQRS.Shared.Events;
using CQRS.Shared.IdentitySupport;
using CQRS.Shared.IdentitySupport.Serialization;
using CQRS.Shared.Storage;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;
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
                    .DependsOn(Dependency.OnValue("connectionString", ConfigurationManager.ConnectionStrings["events"].ConnectionString))
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
                    .DependsOn(Dependency.OnComponent(typeof (IStoreEvents), "eventStore"))
                    .LifestyleTransient(),
                Classes
                    .FromAssemblyContaining<Document>()
                    .BasedOn<AggregateBase>()
                    .WithService.Self()
                    .LifestyleTransient()
                );

            var converter = container.Resolve<IdentityManager>();

            EnableFlatIdMapping(converter);

            MessagesRegistration.RegisterAssembly(typeof(Document).Assembly);
            MessagesRegistration.RegisterAssembly(typeof(DocumentCreated).Assembly);
            SnapshotRegistration.AutomapAggregateState(typeof(DocumentState).Assembly);

            converter.RegisterIdentitiesFromAssembly(typeof(DocumentId).Assembly);
            IdentitiesRegistration.RegisterFromAssembly(typeof(DocumentId).Assembly);

            BsonClassMap.LookupClassMap(typeof (FileId));
            BsonClassMap.LookupClassMap(typeof (FileHandle));

            BsonClassMap.RegisterClassMap<FileNameWithExtension>(m =>
            {
                m.AutoMap();
                m.MapProperty(x => x.FileName).SetElementName("name");
                m.MapProperty(x => x.Extension).SetElementName("ext");
            });
        }

        static void EnableFlatIdMapping(IdentityManager converter)
        {
            EventStoreIdentityBsonSerializer.IdentityConverter = converter;
            BsonClassMap.RegisterClassMap<DomainEvent>(map =>
            {
                map.AutoMap();
                map.MapProperty(x => x.AggregateId).SetSerializer(new EventStoreIdentityBsonSerializer());
            });
            EventStoreIdentityCustomBsonTypeMapper.Register<DocumentId>();
            StringValueCustomBsonTypeMapper.Register<FileId>();
            StringValueCustomBsonTypeMapper.Register<FileHandle>();
            StringValueCustomBsonTypeMapper.Register<FileHash>();
        }
    }
}
