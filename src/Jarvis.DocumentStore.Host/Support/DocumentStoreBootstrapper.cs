using System;
using System.Configuration;
using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.Facilities.Startable;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using CQRS.Kernel.ProjectionEngine;
using Jarvis.DocumentStore.Core.Support;
using Microsoft.Owin.Hosting;

namespace Jarvis.DocumentStore.Host.Support
{
    public class DocumentStoreBootstrapper
    {
        IDisposable _webApplication;
        readonly Uri _serverAddress;
        IWindsorContainer _container;

        public DocumentStoreBootstrapper(Uri serverAddress)
        {
            _serverAddress = serverAddress;
        }

        public void Start()
        {
            _container = new WindsorContainer();
            ContainerAccessor.Instance = _container;
            _container.Kernel.Resolver.AddSubResolver(new CollectionResolver(_container.Kernel, true));
            _container.Kernel.Resolver.AddSubResolver(new ArrayResolver(_container.Kernel, true));
            _container.AddFacility<LoggingFacility>(f => f.UseLog4Net("log4net"));
            _container.AddFacility<StartableFacility>();

            var connectionString = ConfigurationManager.ConnectionStrings["filestore"].ConnectionString;
            _container.Install(
                new ApiInstaller(), 
                new CoreInstaller(connectionString),
                new SchedulerInstaller(connectionString),
                new EventStoreInstaller(),
                new BusInstaller(),
                new ProjectionsInstaller()
            );

            _container.Resolve<ILogger>().InfoFormat("Started server @ {0}", _serverAddress.AbsoluteUri);

            _webApplication = WebApp.Start<DocumentStoreApplication>(_serverAddress.AbsoluteUri);
        }

        public void Stop()
        {
            _webApplication.Dispose();
            _container.Dispose();
        }
    }
}
