using System;
using System.Configuration;
using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.Facilities.Startable;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using CQRS.Kernel.ProjectionEngine;
using CQRS.Shared.Messages;
using Jarvis.DocumentStore.Core.EventHandlers;
using Jarvis.DocumentStore.Core.Support;
using Microsoft.Owin.Hosting;
using Rebus;

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

            var fileStore = ConfigurationManager.ConnectionStrings["filestore"].ConnectionString;
            var sysDb = ConfigurationManager.ConnectionStrings["system"].ConnectionString;

            _container.Install(
                new ApiInstaller(), 
                new CoreInstaller(fileStore, sysDb),
                new SchedulerInstaller(fileStore),
                new EventStoreInstaller(),
                new BusInstaller(),
                new ProjectionsInstaller<NotifyReadModelChanges>()
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

    public class NotifyReadModelChanges : INotifyToSubscribers
    {
        private readonly IBus _bus;

        public NotifyReadModelChanges(IBus bus)
        {
            _bus = bus;
        }

        public void Send(object msg)
        {
            _bus.Publish(msg);
        }
    }
}
