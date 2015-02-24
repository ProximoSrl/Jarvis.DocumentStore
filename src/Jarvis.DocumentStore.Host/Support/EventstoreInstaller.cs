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
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Handle;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Kernel.Engine;
using Jarvis.Framework.Kernel.Engine.Snapshots;
using Jarvis.Framework.Kernel.MultitenantSupport;
using Jarvis.Framework.Kernel.Store;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Shared.IdentitySupport.Serialization;
using Jarvis.Framework.Shared.MultitenantSupport;
using Jarvis.Framework.Shared.Storage;
using Jarvis.NEventStoreEx.CommonDomainEx;
using Jarvis.NEventStoreEx.CommonDomainEx.Persistence.EventStore;
using MongoDB.Bson.Serialization;
using NEventStore;
using NEventStore.Dispatcher;

namespace Jarvis.DocumentStore.Host.Support
{
    public class EventStoreInstaller : IWindsorInstaller
    {
        readonly TenantManager _manager;

        public EventStoreInstaller(TenantManager manager)
        {
            _manager = manager;
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

            MessagesRegistration.RegisterAssembly(typeof(Document).Assembly);
            SnapshotRegistration.AutomapAggregateState(typeof(DocumentState).Assembly);

            identityManager.RegisterIdentitiesFromAssembly(typeof(DocumentId).Assembly);
            IdentitiesRegistration.RegisterFromAssembly(typeof(DocumentId).Assembly);

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

                tenant1.Container.Register(
                    Classes
                        .FromAssemblyContaining<Document>()
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
                                hooks
                                );
                        })
                        .LifestyleSingleton(),
                    Component
                        .For<ICQRSRepository, CQRSRepository>()
                        .ImplementedBy<CQRSRepository>()
                        .Named(tenant.Id + ".repository")
                        .DependsOn(Dependency.OnComponent(typeof (IStoreEvents), esComponentName))
                        .LifestyleTransient()
                    );
            }
        }

        static void RegisterGlobalComponents(IWindsorContainer container)
        {
            container.Register(
                Component
                    .For<IDispatchCommits>()
                    .ImplementedBy<NullDispatcher>(),
                Component
                    .For<ICQRSConstructAggregates>()
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
                    .FromAssemblyContaining<Document>()
                    .BasedOn<AggregateBase>()
                    .WithService.Self()
                    .LifestyleTransient()
                );
        }

        static void EnableFlatIdMapping(IdentityManager converter)
        {
            EventStoreIdentityBsonSerializer.IdentityConverter = converter;
           
        }
    }
}
