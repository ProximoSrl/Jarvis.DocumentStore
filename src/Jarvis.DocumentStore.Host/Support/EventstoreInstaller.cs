using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using NEventStore.Domain;
using NEventStore.Domain.Core;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Kernel.Engine;
using Jarvis.Framework.Kernel.Engine.Snapshots;
using Jarvis.Framework.Kernel.MultitenantSupport;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Shared.IdentitySupport.Serialization;
using Jarvis.Framework.Shared.MultitenantSupport;

using Jarvis.NEventStoreEx.CommonDomainEx;
using Jarvis.NEventStoreEx.CommonDomainEx.Persistence;
using Jarvis.NEventStoreEx.CommonDomainEx.Persistence.EventStore;
using MongoDB.Bson.Serialization;
using NEventStore;
using Jarvis.Framework.Kernel.Support;
using System;
using Jarvis.DocumentStore.Core.Support;
using MongoDB.Driver;
using NEventStore.Persistence.MongoDB;
using NEventStore.Persistence.MongoDB.Support;
using MongoDB.Bson;

namespace Jarvis.DocumentStore.Host.Support
{
    public class EventStoreInstaller : IWindsorInstaller
    {
        readonly TenantManager _manager;
        readonly DocumentStoreConfiguration _config;

        public EventStoreInstaller(TenantManager manager, DocumentStoreConfiguration config)
        {
            _manager = manager;
            _config = config;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            RegisterGlobalComponents(container);
            RegisterTenantServices();
            RegisterMappings(container);
        }

        static void RegisterMappings(IWindsorContainer container)
        {
            var identityManager = container.Resolve<IdentityManager>();

            EnableFlatIdMapping(identityManager);

            MongoRegistration.RegisterAssembly(typeof(DocumentDescriptor).Assembly);
            SnapshotRegistration.AutomapAggregateState(typeof(DocumentDescriptorState).Assembly);

            identityManager.RegisterIdentitiesFromAssembly(typeof(DocumentDescriptorId).Assembly);


            BsonClassMap.LookupClassMap(typeof(BlobId));
            BsonClassMap.LookupClassMap(typeof(DocumentHandle));

            BsonClassMap.RegisterClassMap<FileNameWithExtension>(m =>
            {
                m.AutoMap();
                m.MapProperty(x => x.FileName).SetElementName("name");
                m.MapProperty(x => x.Extension).SetElementName("ext");
            });
        }

        private void RegisterTenantServices()
        {
            foreach (var tenant in _manager.Tenants)
            {
                ITenant tenant1 = tenant;

                var esComponentName = tenant.Id + "-es";
                var readModelDb = tenant1.Get<IMongoDatabase>("readmodel.db");

                var mongoPersistenceOptions = new MongoPersistenceOptions();
                mongoPersistenceOptions.DisableSnapshotSupport = true;
                mongoPersistenceOptions.ConcurrencyStrategy = ConcurrencyExceptionStrategy.FillHole;
                var nesUrl = new MongoUrl(tenant1.GetConnectionString("events"));
                var nesDb = new MongoClient(nesUrl).GetDatabase(nesUrl.DatabaseName);
                var eventsCollection = nesDb.GetCollection<BsonDocument>("Commits");
                mongoPersistenceOptions.CheckpointGenerator = new InMemoryCheckpointGenerator(eventsCollection);

                tenant1.Container.Register(
                            Classes
                                .FromAssemblyContaining<DocumentDescriptor>()
                                .BasedOn<IPipelineHook>()
                                .WithServiceAllInterfaces(),
                            Component
                                .For<IStoreEvents>()
                                .Named(esComponentName)
                                .UsingFactory<EventStoreFactory, IStoreEvents>(f =>
                                {
                                    var hooks = tenant1.Container.ResolveAll<IPipelineHook>();

                                    return f.BuildEventStore(
                                            tenant1.GetConnectionString("events"),
                                            hooks,
                                            mongoPersistenceOptions : mongoPersistenceOptions
                                        );
                                })
                                .LifestyleSingleton(),

                            Component
                                .For<IRepositoryEx, RepositoryEx>()
                                .ImplementedBy<RepositoryEx>()
                                .Named(tenant.Id + ".repository")
                                .DependsOn(Dependency.OnComponent(typeof(IStoreEvents), esComponentName))
                                .LifestyleTransient(),

                            Component
                                .For<Func<IRepositoryEx>>()
                                .Instance(() => tenant1.Container.Resolve<IRepositoryEx>()),
                            Component
                                .For<ISnapshotManager>()
                                .DependsOn(Dependency.OnValue("cacheEnabled", _config.EnableSnapshotCache))
                                .ImplementedBy<CachedSnapshotManager>(),
                            Component
                                .For<IAggregateCachedRepositoryFactory>()
                                .ImplementedBy<AggregateCachedRepositoryFactory>()
                                .DependsOn(Dependency.OnValue("cacheDisabled", false)),
                            Component
                                .For<ISnapshotPersistenceStrategy>()
                                .ImplementedBy<NumberOfCommitsShapshotPersistenceStrategy>()
                                .DependsOn(Dependency.OnValue("commitsThreshold", 100)),
                             Component.For<ISnapshotPersister>()
                                    .ImplementedBy<MongoSnapshotPersisterProvider>()
                                    .DependsOn(Dependency.OnValue<IMongoDatabase>(readModelDb))
                            );
            }
        }

        static void RegisterGlobalComponents(IWindsorContainer container)
        {
            container.Register(
                Component
                    .For<IConstructAggregatesEx>()
                    .ImplementedBy<AggregateFactory>(),
                Component
                    .For<EventStoreFactory>()
                    .DependsOn()
                    .LifestyleSingleton(),
                Component
                    .For<IDetectConflicts>()
                    .ImplementedBy<ConflictDetector>()
                    .LifestyleTransient(),
                Component
                    .For<ICounterService>()
                    .ImplementedBy<MultitenantCounterService>(),
                Component
                    .For<IIdentityManager, IIdentityGenerator, IIdentityConverter, IdentityManager>()
                    .ImplementedBy<IdentityManager>(),
                Classes
                    .FromAssemblyContaining<DocumentDescriptor>()
                    .BasedOn<AggregateBase>()
                    .WithService.Self()
                    .LifestyleTransient()
                );
        }

        static void EnableFlatIdMapping(IdentityManager converter)
        {
            MongoFlatIdSerializerHelper.Initialize(converter);
        }
    }
}
