using System.Configuration;
using Castle.Facilities.Startable;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using CQRS.Kernel.Engine.Snapshots;
using CQRS.Kernel.Events;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Kernel.ProjectionEngine;
using CQRS.Kernel.ProjectionEngine.Client;
using CQRS.Kernel.ProjectionEngine.RecycleBin;
using CQRS.Shared.IdentitySupport;
using CQRS.Shared.Messages;
using CQRS.Shared.MultitenantSupport;
using CQRS.Shared.ReadModel;
using CQRS.Shared.Storage;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.EventHandlers;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Core.Support
{
    public class ProjectionsInstaller<TNotifier> : IWindsorInstaller where TNotifier : INotifyToSubscribers
    {
        readonly TenantManager _manager;

        public ProjectionsInstaller(TenantManager manager)
        {
            _manager = manager;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // add rm prefix to collections
            CollectionNames.Customize = n => "rm." + n;

            RegisterGlobalComponents(container);

            foreach (ITenant tenant in _manager.Tenants)
            {
                var config = new ProjectionEngineConfig
                {
                    EventStoreConnectionString = tenant.GetConnectionString("events"),
                    Slots = ConfigurationManager.AppSettings["engine-slots"].Split(','),
                    PollingMsInterval = int.Parse(ConfigurationManager.AppSettings["polling-interval-ms"]),
                    ForcedGcSecondsInterval = int.Parse(ConfigurationManager.AppSettings["memory-collect-seconds"])
                };

                ITenant tenant1 = tenant;
                var readModelDb = tenant.Get<MongoDatabase>("db.readmodel");

                container.Register(
                    Component
                        .For<ConcurrentProjectionsEngine>()
                        .Named(tenant.Id + ".prjengine")
                        .LifestyleTransient()
                        .DependsOn(Dependency.OnValue<ProjectionEngineConfig>(config))
                        .StartUsingMethod(x => x.Start)
                        .StopUsingMethod(x => x.Stop),
                    Classes
                        .FromAssemblyContaining<DocumentProjection>()
                        .BasedOn<IProjection>()
                        .Configure(r => r
                            .Named(tenant1.Id + ".prj." + r.Implementation.FullName)
                            .DependsOn(Dependency.OnValue<TenantId>(tenant1.Id))
                        )
                        .WithServiceAllInterfaces()
                        .LifestyleTransient(),
                    Component
                        .For<TenantProjections>()
                        .DependsOn(Dependency.OnValue<TenantId>(tenant1.Id))
                        .Named(tenant1.Id+".projections")
                        .LifestyleTransient(),
                    Component
                        .For<IInitializeReadModelDb>()
                        .ImplementedBy<InitializeReadModelDb>()
                        .Named(tenant.Id+".readmodel.initializer")
                        .LifestyleTransient(),
                    Component
                        .For<IConcurrentCheckpointTracker>()
                        .ImplementedBy<ConcurrentCheckpointTracker>()
                        .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb))
                        .Named(tenant.Id + ".checkpoint.tracker")
                        .LifestyleTransient(),
                    Component
                        .For(new[]
                        {
                            typeof (ICollectionWrapper<,>),
                            typeof (IReadOnlyCollectionWrapper<,>)
                        })
                        .LifestyleTransient()
                        .ImplementedBy(typeof(CollectionWrapper<,>))
                        .Named(tenant.Id + ".collection.wrappers")
                        .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb)),
                    Component
                        .For(typeof (IReader<,>), typeof (IMongoDbReader<,>))
                        .ImplementedBy(typeof (MongoReaderForProjections<,>))
                        .Named(tenant.Id + ".collection.readers")
                        .LifestyleTransient()
                        .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb)),
                    Component
                        .For<IPollingClient>()
                        .ImplementedBy<PollingClientWrapper>()
                        .LifestyleTransient()
                        .Named(tenant.Id + ".pollingclient")
                        .DependsOn(Dependency.OnConfigValue("boost", ConfigurationManager.AppSettings["engine-multithread"])),
                    Component
                        .For<IRebuildContext>()
                        .ImplementedBy<RebuildContext>()
                        .Named(tenant.Id + ".rebuildcontext")
                        .LifestyleTransient()
                        .DependsOn(Dependency.OnValue<bool>(RebuildSettings.NitroMode)),
                    Component
                        .For<IMongoStorageFactory>()
                        .ImplementedBy<MongoStorageFactory>()
                        .Named(tenant.Id + ".storage.factory")
                        .LifestyleTransient()
                        .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb)),
                    Component
                        .For<IRecycleBin>()
                        .ImplementedBy<RecycleBin>()
                        .Named(tenant.Id + ".recycleBin")
                        .LifestyleTransient()
                        .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb))
                    );
            }


            var im = container.Resolve<IdentityManager>();
            IdentitiesRegistration.RegisterFromAssembly(typeof (DocumentId).Assembly);

            MessagesRegistration.RegisterAssembly(typeof (DocumentId).Assembly);
            SnapshotRegistration.AutomapAggregateState(typeof (DocumentState).Assembly);

            im.RegisterIdentitiesFromAssembly(typeof (DocumentId).Assembly);

#if DEBUG
            {
                var eng = container.Resolve<ConcurrentProjectionsEngine>();
            }
#endif
        }

        static void RegisterGlobalComponents(IWindsorContainer container)
        {
            container.Register(
                Component
                    .For<IHousekeeper>()
                    .ImplementedBy<NullHouseKeeper>(),
                Component
                    .For<INotifyToSubscribers>()
                    .ImplementedBy<TNotifier>(),
                Component
                    .For<CommitEnhancer>(),
                Component
                    .For<INotifyCommitHandled>()
                    .ImplementedBy<NullNotifyCommitHandled>()
                );
        }
    }
}