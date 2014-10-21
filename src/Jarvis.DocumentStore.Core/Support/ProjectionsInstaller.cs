using System.Configuration;
using Castle.Facilities.Startable;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using CQRS.Kernel.Engine.Snapshots;
using CQRS.Kernel.Events;
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
        readonly ITenant _tenant;

        public ProjectionsInstaller(ITenant tenant)
        {
            _tenant = tenant;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // add rm prefix to collections
            CollectionNames.Customize = n => "rm." + n;

            var config = new ProjectionEngineConfig
            {
                EventStoreConnectionString = _tenant.GetConnectionString("events"),
                Slots = ConfigurationManager.AppSettings["engine-slots"].Split(','),
                PollingMsInterval = int.Parse(ConfigurationManager.AppSettings["polling-interval-ms"]),
                ForcedGcSecondsInterval = int.Parse(ConfigurationManager.AppSettings["memory-collect-seconds"]),
                TenantId = _tenant.Id
            };

            var readModelDb = _tenant.Get<MongoDatabase>("db.readmodel");

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
                    .ImplementedBy<NullNotifyCommitHandled>(),
                Component
                    .For<ConcurrentProjectionsEngine>()
                    .LifestyleSingleton()
                    .DependsOn(Dependency.OnValue<ProjectionEngineConfig>(config))
                    .StartUsingMethod(x => x.Start)
                    .StopUsingMethod(x => x.Stop),
                Classes
                    .FromAssemblyContaining<DocumentProjection>()
                    .BasedOn<IProjection>()
                    .Configure(r => r
                        .DependsOn(Dependency.OnValue<TenantId>(_tenant.Id))
                    )
                    .WithServiceAllInterfaces()
                    .LifestyleSingleton(),
                Component
                    .For<IInitializeReadModelDb>()
                    .ImplementedBy<InitializeReadModelDb>()
                    .LifestyleTransient(),
                Component
                    .For<IConcurrentCheckpointTracker>()
                    .ImplementedBy<ConcurrentCheckpointTracker>()
                    .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb))
                    .LifestyleTransient(),
                Component
                    .For(new[]
                    {
                        typeof (ICollectionWrapper<,>),
                        typeof (IReadOnlyCollectionWrapper<,>)
                    })
                    .LifestyleTransient()
                    .ImplementedBy(typeof(CollectionWrapper<,>))
                    .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb)),
                Component
                    .For(typeof(IReader<,>), typeof(IMongoDbReader<,>))
                    .ImplementedBy(typeof(MongoReaderForProjections<,>))
                    .LifestyleTransient()
                    .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb)),
                Component
                    .For<IPollingClient>()
                    .ImplementedBy<PollingClientWrapper>()
                    .LifestyleTransient()
                    .DependsOn(Dependency.OnConfigValue("boost", ConfigurationManager.AppSettings["engine-multithread"])),
                Component
                    .For<IRebuildContext>()
                    .ImplementedBy<RebuildContext>()
                    .LifestyleTransient()
                    .DependsOn(Dependency.OnValue<bool>(RebuildSettings.NitroMode)),
                Component
                    .For<IMongoStorageFactory>()
                    .ImplementedBy<MongoStorageFactory>()
                    .LifestyleTransient()
                    .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb)),
                Component
                    .For<IRecycleBin>()
                    .ImplementedBy<RecycleBin>()
                    .LifestyleTransient()
                    .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb))
                );

#if DEBUG
            container.Resolve<ConcurrentProjectionsEngine>();
#endif
        }
    }
}
