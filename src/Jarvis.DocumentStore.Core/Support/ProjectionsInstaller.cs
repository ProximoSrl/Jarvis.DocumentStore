using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using CQRS.Kernel.Events;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Core.Support
{
    public class ProjectionsInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            var mongoUrl = new MongoUrl(ConfigurationManager.ConnectionStrings["readmodel"].ConnectionString);

            var mongoClient = new MongoClient(mongoUrl);
            var readModelDb = mongoClient.GetServer().GetDatabase(mongoUrl.DatabaseName);

            container.Register(
                Component
                    .For<ConcurrentProjectionsEngine>()
                    .StartUsingMethod(x => x.Start)
                    .StopUsingMethod(x => x.Stop),
                Component
                    .For<IConcurrentCheckpointTracker>()
                    .ImplementedBy<ConcurrentCheckpointTracker>(),
                Component
                    .For<IProjection>()
                    .ImplementedBy<DispatchEventsOnBusProjection>(),
                Component
                    .For<IPollingClient>()
                    .ImplementedBy<PollingClientWrapper>()
                    .DependsOn(Dependency.OnConfigValue("boost", ConfigurationManager.AppSettings["engine-multithread"])),
                Component
                    .For<CommitEnhancer>()
            );
        }
    }
}
