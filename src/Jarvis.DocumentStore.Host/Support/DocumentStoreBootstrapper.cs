using System;
using System.Configuration;
using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.Facilities.Startable;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Services.Logging.Log4netIntegration;
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
        ILogger _logger;

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
            _container.AddFacility<LoggingFacility>(f => f.LogUsing(new ExtendedLog4netFactory("log4net")));
            _container.AddFacility<StartableFacility>();

            var fileStore = ConfigurationManager.ConnectionStrings["filestore"].ConnectionString;
            var sysDb = ConfigurationManager.ConnectionStrings["system"].ConnectionString;

            _logger = _container.Resolve<ILoggerFactory>().Create(GetType());
            _logger.InfoFormat("Started server @ {0}", _serverAddress.AbsoluteUri);

            _container.Install(
                new CoreInstaller(fileStore, sysDb),
                new EventStoreInstaller(),
                new BusInstaller()
            );

            if (RolesHelper.IsWorker)
            {
                _logger.Debug("Configured Scheduler");
                _container.Install(new SchedulerInstaller(fileStore));
            }

            if (RolesHelper.IsReadmodelBuilder)
            {
                _logger.Debug("Configured Projections");
                _container.Install(new ProjectionsInstaller<NotifyReadModelChanges>());
            }


            if (RolesHelper.IsApiServer)
            {
                _logger.Debug("Configured API server");
                _container.Install(new ApiInstaller());
                _webApplication = WebApp.Start<DocumentStoreApplication>(_serverAddress.AbsoluteUri);
            }

            //try
            //{
            //    throw new Exception("WROOOOOONG!");
            //}
            //catch (Exception ex)
            //{
            //    _logger.ErrorFormat(ex, "something went damn wrong");
            //}
        }

        public void Stop()
        {
            if (_webApplication != null)
            {
                _webApplication.Dispose();
            }

            foreach (var act in _container.ResolveAll<IShutdownActivity>())
            {
                _logger.DebugFormat("Shutting down activity: {0}", act.GetType().FullName);
                try
                {
                    act.Shutdown();
                }
                catch (Exception ex)
                {
                    _logger.ErrorFormat(ex, "Shutting down {0}", act.GetType().FullName);
                }
            }

            _container.Dispose();
        }
    }

    public class NotifyReadModelChanges : INotifyToSubscribers
    {
        public NotifyReadModelChanges()
        {
        }

        public void Send(object msg)
        {
        }
    }
}
