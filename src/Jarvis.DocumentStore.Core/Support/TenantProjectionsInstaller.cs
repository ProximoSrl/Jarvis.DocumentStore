using System.Configuration;
using Castle.Facilities.Startable;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Jarvis.DocumentStore.Core.EventHandlers;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.Framework.Kernel.Events;
using Jarvis.Framework.Kernel.ProjectionEngine;
using Jarvis.Framework.Kernel.ProjectionEngine.Client;
using Jarvis.Framework.Kernel.ProjectionEngine.RecycleBin;
using Jarvis.Framework.Shared.Messages;
using Jarvis.Framework.Shared.MultitenantSupport;
using Jarvis.Framework.Shared.ReadModel;
using MongoDB.Driver;
using Castle.Core.Logging;

namespace Jarvis.DocumentStore.Core.Support
{
    public class TenantProjectionsInstaller<TNotifier> : IWindsorInstaller where TNotifier : INotifyToSubscribers
    {
        readonly ITenant _tenant;
        readonly DocumentStoreConfiguration _config;

        public TenantProjectionsInstaller(
            ITenant tenant, 
            DocumentStoreConfiguration config)
        {
            _tenant = tenant;
            _config = config;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // add rm prefix to collections
            CollectionNames.Customize = n => "rm." + n;

            var config = new ProjectionEngineConfig
            {
                EventStoreConnectionString = _tenant.GetConnectionString("events"),
                Slots = _config.EngineSlots,
                PollingMsInterval = _config.PollingMsInterval,
                ForcedGcSecondsInterval = _config.ForcedGcSecondsInterval,
                TenantId = _tenant.Id,
                DelayedStartInMilliseconds = _config.DelayedStartInMilliseconds,
            };

            var readModelDb = _tenant.Get<MongoDatabase>("readmodel.db");


            container.Register(
                Component
                    .For<IDocumentWriter>()
                    .ImplementedBy<DocumentWriter>()
                    .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb)),
                Component
                    .For(typeof(IReader<,>), typeof(IMongoDbReader<,>))
                    .ImplementedBy(typeof(MongoReaderForProjections<,>))
                    .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb))
            );
            
            if (!_config.IsReadmodelBuilder)
                return;

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
                    .ImplementedBy<InitializeReadModelDb>(),
                Component
                    .For<IConcurrentCheckpointTracker>()
                    .ImplementedBy<ConcurrentCheckpointTracker>()
                    .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb)),
                Component
                    .For(new[]
                    {
                        typeof (ICollectionWrapper<,>),
                        typeof (IReadOnlyCollectionWrapper<,>)
                    })
                    .ImplementedBy(typeof(CollectionWrapper<,>))
                    .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb)),
                Component
                    .For<IPollingClient>()
                    .ImplementedBy<PollingClientWrapper>()
                    .DependsOn(Dependency.OnConfigValue("boost", _config.Boost)),
                Component
                    .For<IRebuildContext>()
                    .ImplementedBy<RebuildContext>()
                    .DependsOn(Dependency.OnValue<bool>(RebuildSettings.NitroMode)),
                Component
                    .For<IMongoStorageFactory>()
                    .ImplementedBy<MongoStorageFactory>()
                    .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb)),
                Component
                    .For<DocumentDescriptorByHashReader>(),
                Component
                    .For<DeduplicationHelper>(),
                Component
                    .For<IRecycleBin>()
                    .ImplementedBy<RecycleBin>()
                    .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb))
                );

            //This registration made the entire ConcurrentProjectionEngine starts
            //so it is better to register after all the other components are registered
            //correctly.
            container.Register(
                Component
                    .For<ConcurrentProjectionsEngine,ITriggerProjectionsUpdate>()
                    .ImplementedBy<ConcurrentProjectionsEngine>()
                    .LifestyleSingleton()
                    .DependsOn(Dependency.OnValue<ProjectionEngineConfig>(config))
                    .StartUsingMethod(x => x.Start)
                    .StopUsingMethod(x => x.Stop)
                );

#if DEBUG
            container.Resolve<ConcurrentProjectionsEngine>();
#endif
        }
    }
}
