using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.Facilities.Startable;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Services.Logging.Log4netIntegration;
using Castle.Windsor;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Kernel.ProjectionEngine;
using CQRS.Shared.IdentitySupport;
using CQRS.Shared.Messages;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.EventHandlers;
using Jarvis.DocumentStore.Core.Support;
using Microsoft.Owin.Hosting;

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

        public void Start(DocumentStoreConfiguration roles)
        {
            _container = new WindsorContainer();
            ContainerAccessor.Instance = _container;
            _container.Kernel.Resolver.AddSubResolver(new CollectionResolver(_container.Kernel, true));
            _container.Kernel.Resolver.AddSubResolver(new ArrayResolver(_container.Kernel, true));
//            _container.Kernel.Resolver.AddSubResolver(new MultiTenantSubDependencyResolver(_container.Kernel));
            _container.AddFacility<LoggingFacility>(f => f.LogUsing(new ExtendedLog4netFactory("log4net")));
            _container.AddFacility<StartableFacility>();
            _container.AddFacility<TypedFactoryFacility>();

            var quartz = ConfigurationManager.ConnectionStrings["ds.quartz"].ConnectionString;

            _logger = _container.Resolve<ILoggerFactory>().Create(GetType());
            _logger.InfoFormat("Started server @ {0}", _serverAddress.AbsoluteUri);

            var manager = BuildTenants(_container);

            var installers = new List<IWindsorInstaller>()
            {
                new CoreInstaller(),
                new EventStoreInstaller(manager),
                new SchedulerInstaller(quartz, roles.IsWorker)
            };

            _logger.Debug("Configured Scheduler");

            if (roles.IsApiServer)
            {
                installers.Add(new ApiInstaller());
                
                _webApplication = WebApp.Start<DocumentStoreApplication>(_serverAddress.AbsoluteUri);
                _logger.Debug("Configured API server");
            }

            _container.Install(installers.ToArray());

            foreach (var tenant in manager.Tenants)
            {
                var tenantInstallers = new List<IWindsorInstaller>
                {
                    new CoreTenantInstaller(),
                    new HandlersInstaller(),
                    new TenantJobsInstaller()
                };

                if (roles.IsApiServer)
                {
                    tenantInstallers.Add(new TenantApiInstaller());
                }

                if (roles.IsReadmodelBuilder)
                {
                    tenantInstallers.Add(new ProjectionsInstaller<NotifyReadModelChanges>(tenant));
                    _logger.DebugFormat("Configured Projections for tenant {0}", tenant.Id);
                }
                
                tenant.Container.Install(tenantInstallers.ToArray());
            }

        }

        TenantManager BuildTenants(IWindsorContainer container)
        {
            _logger.Debug("Configuring tenants");
            var manager = new TenantManager(container.Kernel);
            container.Register(Component.For<ITenantAccessor, TenantManager>().Instance(manager));

            var tenants = ConfigurationManager.AppSettings["tenants"].Split(',').Select(x=> x.Trim()).ToArray();

            foreach (var tenantId in tenants)
            {
                _logger.DebugFormat("Adding tenant {0}", tenantId);

                var settings = new DocumentStoreTenantSettings(tenantId);

                var tenant = manager.AddTenant(settings);
                tenant.Container.Kernel.Resolver.AddSubResolver(new CollectionResolver(tenant.Container.Kernel, true));
                tenant.Container.Kernel.Resolver.AddSubResolver(new ArrayResolver(tenant.Container.Kernel, true));
                tenant.Container.AddFacility<StartableFacility>();

                container.AddChildContainer(tenant.Container);
            }

            return manager;
        }

        public T Resolve<T>()
        {
            return _container.Resolve<T>();
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
