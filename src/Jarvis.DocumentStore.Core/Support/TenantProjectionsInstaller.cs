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
using Jarvis.DocumentStore.Core.Jobs;
using System;
using NEventStore.Persistence;
using Jarvis.DocumentStore.Core.EvenstoreHooks;

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
                EngineVersion = _config.EngineVersion,
                BucketInfo = _config.BucketInfo,
            };

          

            var readModelDb = _tenant.Get<IMongoDatabase>("readmodel.db");

            container.Register(
                Component
                    .For<IDocumentWriter>()
                    .ImplementedBy<DocumentWriter>()
                    .DependsOn(Dependency.OnValue<IMongoDatabase>(readModelDb)),
                Component
                    .For(typeof(IReader<,>), typeof(IMongoDbReader<,>))
                    .ImplementedBy(typeof(MongoReaderForProjections<,>))
                    .DependsOn(Dependency.OnValue<IMongoDatabase>(readModelDb))
            );

            container.Register(
               Component
                   .For<IHousekeeper>()
                   .ImplementedBy<NullHouseKeeper>(),
               Component
                   .For<INotifyToSubscribers>()
                   .ImplementedBy<TNotifier>(),
               Component
                       .For<ICommitEnhancer>()
                       .ImplementedBy<CommitEnhancer>(),
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
                   .DependsOn(Dependency.OnValue<IMongoDatabase>(readModelDb)),
               Component
                   .For(new[]
                   {
                        typeof (ICollectionWrapper<,>),
                        typeof (IReadOnlyCollectionWrapper<,>)
                   })
                   .ImplementedBy(typeof(CollectionWrapper<,>))
                   .DependsOn(Dependency.OnValue<IMongoDatabase>(readModelDb)),
               Component
                   .For<IRebuildContext>()
                   .ImplementedBy<RebuildContext>()
                   .DependsOn(Dependency.OnValue<bool>(RebuildSettings.NitroMode)),
               Component
                   .For<IMongoStorageFactory>()
                   .ImplementedBy<MongoStorageFactory>()
                   .DependsOn(Dependency.OnValue<IMongoDatabase>(readModelDb)),
               Component
                   .For<DocumentDescriptorByHashReader>(),
               Component
                   .For<DeduplicationHelper>(),
               Component
                   .For<IRecycleBin>()
                   .ImplementedBy<RecycleBin>()
                   .DependsOn(Dependency.OnValue<IMongoDatabase>(readModelDb))
               );

            if (!_config.IsReadmodelBuilder)
                return;


            //This registration made the entire ConcurrentProjectionEngine starts
            //so it is better to register after all the other components are registered
            //correctly.
            if (config.EngineVersion == "v1")
            {
                throw new NotSupportedException("V1 projection engine not supported anymore");
                //container.Register(
                //    Component
                //      .For<IPollingClient>()
                //      .ImplementedBy<PollingClientWrapper>()
                //      .DependsOn(Dependency.OnConfigValue("boost", _config.Boost)),
                //    Component
                //        .For<ConcurrentProjectionsEngine, ITriggerProjectionsUpdate>()
                //        .ImplementedBy<ConcurrentProjectionsEngine>()
                //        .LifestyleSingleton()
                //        .DependsOn(Dependency.OnValue<ProjectionEngineConfig>(config))
                //        .StartUsingMethod(x => x.Start)
                //        .StopUsingMethod(x => x.Stop)
                //);
            }
            else if (config.EngineVersion == "v2")
            {
                throw new NotSupportedException("V2 projection engine not supported anymore because of NES6 dropping standard commit polling client");
                //container.Register(
                //    Component.For<ProjectionEngineConfig>()
                //        .Instance(config),
                //   Component
                //       .For<ICommitPollingClient>()
                //       .ImplementedBy<CommitPollingClient>()
                //       .DependsOn(Dependency.OnValue("id", "Main-Poller"))
                //       .LifeStyle.Transient,
                //    Component
                //        .For<Func<IPersistStreams, ICommitPollingClient>>()
                //        .Instance(ps => container.Resolve<ICommitPollingClient>(new { persistStreams = ps })),
                //  Component
                //        .For<ProjectionEngine, ITriggerProjectionsUpdate>()
                //        .ImplementedBy<ProjectionEngine>()
                //        .LifestyleSingleton()
                //        .StartUsingMethod(x => x.Start)
                //        .StopUsingMethod(x => x.Stop)
                //        );
            }
            else if (config.EngineVersion == "v3")
            {
                container.Register(
                    Component.For<ProjectionEngineConfig>()
                        .Instance(config),
                   Component
                       .For<ICommitPollingClient>()
                       .ImplementedBy<CommitPollingClient2>()
                       .DependsOn(Dependency.OnValue("id", "Main-Poller"))
                       .LifeStyle.Transient,
                  Component
                        .For<ProjectionEngine, ITriggerProjectionsUpdate>()
                        .ImplementedBy<ProjectionEngine>()
                        .LifestyleSingleton()
                        .StartUsingMethod(x => x.Start)
                        .StopUsingMethod(x => x.Stop)
                        );
            }
        }
    }
}
