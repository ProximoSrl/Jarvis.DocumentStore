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

            var logger = _container.Resolve<ILoggerFactory>().Create(GetType());
            logger.InfoFormat("Started server @ {0}", _serverAddress.AbsoluteUri);

            _container.Install(
                new CoreInstaller(fileStore, sysDb),
                new EventStoreInstaller(),
                new BusInstaller()
            );

            if (RolesHelper.IsWorker)
            {
                logger.Debug("Configured Scheduler");
                _container.Install(new SchedulerInstaller(fileStore));
            }

            if (RolesHelper.IsReadmodelBuilder)
            {
                logger.Debug("Configured Projections");
                _container.Install(new ProjectionsInstaller<NotifyReadModelChanges>());
            }


            if (RolesHelper.IsApiServer)
            {
                logger.Debug("Configured API server");
                _container.Install(new ApiInstaller());
                _webApplication = WebApp.Start<DocumentStoreApplication>(_serverAddress.AbsoluteUri);
            }
        }

        public void Stop()
        {
            if (_webApplication != null)
            {
                _webApplication.Dispose();
            }
            
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
