﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using CQRS.Shared.ReadModel;
using CQRS.Shared.Storage;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.EventHandlers;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Core.Support
{
    public class ProjectionsInstaller<TNotifier> : IWindsorInstaller where TNotifier :INotifyToSubscribers
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            var mongoUrl = new MongoUrl(ConfigurationManager.ConnectionStrings["readmodel"].ConnectionString);
            var mongoClient = new MongoClient(mongoUrl);
            var readModelDb = mongoClient.GetServer().GetDatabase(mongoUrl.DatabaseName);

            // add rm prefix to collections
            CollectionNames.Customize = n => "rm." + n;

            var config = new ProjectionEngineConfig()
            {
                EventStoreConnectionString = ConfigurationManager.ConnectionStrings["events"].ConnectionString,
                Slots = ConfigurationManager.AppSettings["engine-slots"].Split(','),
                PollingMsInterval = int.Parse(ConfigurationManager.AppSettings["polling-interval-ms"]),
                ForcedGcSecondsInterval = int.Parse(ConfigurationManager.AppSettings["memory-collect-seconds"])
            };

            container.Register(
                Component
                    .For<ConcurrentProjectionsEngine>()
                    .DependsOn(Dependency.OnValue<ProjectionEngineConfig>(config))
                    .StartUsingMethod(x => x.Start)
                    .StopUsingMethod(x => x.Stop),
                Classes
                    .FromAssemblyContaining<DocumentProjection>()
                    .BasedOn<IProjection>()
                    .WithServiceAllInterfaces(),
                Component
                    .For<IInitializeReadModelDb>()
                    .ImplementedBy<InitializeReadModelDb>(),
                Component
                    .For<IConcurrentCheckpointTracker>()
                    .ImplementedBy<ConcurrentCheckpointTracker>(),
                Component
                    .For<IHousekeeper>()
                    .ImplementedBy<NullHouseKeeper>(),
                Component
                    .For<INotifyToSubscribers>()
                    .ImplementedBy<TNotifier>(),
                Component
                    .For(new Type[]
                        {
                            typeof (ICollectionWrapper<,>),
                            typeof (IReadOnlyCollectionWrapper<,>)
                        })
                    .ImplementedBy(typeof(CollectionWrapper<,>))
                    .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb)),
                Component
                    .For(typeof(IReader<,>), typeof(IMongoDbReader<,>))
                    .ImplementedBy(typeof(MongoReaderForProjections<,>))
                    .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb)),
                //Component
                //    .For<IProjection>()
                //    .ImplementedBy<DispatchEventsOnBusProjection>(),
                Component
                    .For<IPollingClient>()
                    .ImplementedBy<PollingClientWrapper>()
                    .DependsOn(Dependency.OnConfigValue("boost", ConfigurationManager.AppSettings["engine-multithread"])),
                Component
                    .For<CommitEnhancer>(),
                Component
                    .For<INotifyCommitHandled>()
                    .ImplementedBy<NullNotifyCommitHandled>(),
                Component
                    .For<IRebuildContext>()
                    .ImplementedBy<RebuildContext>()
                    .DependsOn(Dependency.OnValue<bool>(RebuildSettings.NitroMode)),
                Component
                    .For<IMongoStorageFactory>()
                    .ImplementedBy<MongoStorageFactory>(),
                Component
                    .For<IRecycleBin>()
                    .ImplementedBy<RecycleBin>()
                    .DependsOn(Dependency.OnValue<MongoDatabase>(readModelDb))
            );

            var im = container.Resolve<IdentityManager>();
            IdentitiesRegistration.RegisterFromAssembly(typeof(DocumentId).Assembly);

            MessagesRegistration.RegisterAssembly(typeof(DocumentId).Assembly);
            SnapshotRegistration.AutomapAggregateState(typeof(DocumentState).Assembly);

            im.RegisterIdentitiesFromAssembly(typeof(DocumentId).Assembly);

#if DEBUG
            {
                var eng = container.Resolve<ConcurrentProjectionsEngine>();
            }
#endif
        }
    }
}
