﻿using System;
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
using Jarvis.ConfigurationService.Client;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.Framework.Kernel.MultitenantSupport;
using Jarvis.Framework.Shared.Messages;
using Jarvis.Framework.Shared.MultitenantSupport;
using Microsoft.Owin.Hosting;
using Rebus.Logging;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;

namespace Jarvis.DocumentStore.Host.Support
{
    public class DocumentStoreBootstrapper
    {
        IDisposable _webApplication;
        Uri _serverAddress;
        IWindsorContainer _container;
        ILogger _logger;

        public void Start(DocumentStoreConfiguration config)
        {
            _serverAddress = config.ServerAddress;
            BuildContainer(config);

            _logger.DebugFormat(
                "Roles:\n  api: {0}\n  worker : {1}\n  projections: {2}",
                config.IsApiServer,
                config.IsWorker,
                config.IsReadmodelBuilder
            );

            var manager = BuildTenants(_container, config);

            var installers = new List<IWindsorInstaller>()
            {
                new CoreInstaller(config),
                new EventStoreInstaller(manager),
                new SchedulerInstaller(config.QuartzConnectionString, false),
                new QueueInfrasctructureInstaller(config.QueueConnectionString, config.QueueInfoList),
            };

            _logger.Debug("Configured Scheduler");

            if (config.IsApiServer)
            {
                installers.Add(new ApiInstaller());
                
                _webApplication = WebApp.Start<DocumentStoreApplication>(_serverAddress.AbsoluteUri);
                _logger.InfoFormat("Started server @ {0}", _serverAddress.AbsoluteUri);
            }

            _container.Install(installers.ToArray());
            foreach (var tenant in manager.Tenants)
            {
                var tenantInstallers = new List<IWindsorInstaller>
                {
                    new TenantCoreInstaller(tenant),
                    new TenantHandlersInstaller(tenant),
                    new TenantJobsInstaller(tenant)
                };

                if (config.IsApiServer)
                {
                    tenantInstallers.Add(new TenantApiInstaller())                    ;
                }

                tenantInstallers.Add(new TenantProjectionsInstaller<NotifyReadModelChanges>(tenant, config.IsReadmodelBuilder));
                _logger.DebugFormat("Configured Projections for tenant {0}", tenant.Id);
                
                tenant.Container.Install(tenantInstallers.ToArray());
            }
            foreach (var act in _container.ResolveAll<IStartupActivity>())
            {
                _logger.DebugFormat("Starting activity: {0}", act.GetType().FullName);
                try
                {
                    act.Start();
                }
                catch (Exception ex)
                {
                    _logger.ErrorFormat(ex, "Shutting down {0}", act.GetType().FullName);
                }
            }
        }

        void BuildContainer(DocumentStoreConfiguration config)
        {
            _container = new WindsorContainer();
            ContainerAccessor.Instance = _container;
            _container.Register(Component.For<DocumentStoreConfiguration>().Instance(config));
            _container.Kernel.Resolver.AddSubResolver(new CollectionResolver(_container.Kernel, true));
            _container.Kernel.Resolver.AddSubResolver(new ArrayResolver(_container.Kernel, true));


            _container.AddFacility<LoggingFacility>(config.CreateLoggingFacility);

            _container.AddFacility<StartableFacility>();
            _container.AddFacility<TypedFactoryFacility>();

            _logger = _container.Resolve<ILoggerFactory>().Create(GetType());

        }

        TenantManager BuildTenants(IWindsorContainer container, DocumentStoreConfiguration config)
        {
            _logger.Debug("Configuring tenants");
            var manager = new TenantManager(container.Kernel);
            container.Register(Component.For<ITenantAccessor, TenantManager>().Instance(manager));

            foreach (var settings in config.TenantSettings)
            {
                _logger.DebugFormat("Adding tenant {0}", settings.TenantId);

                var tenant = manager.AddTenant(settings);
                tenant.Container.Kernel.Resolver.AddSubResolver(new CollectionResolver(tenant.Container.Kernel, true));
                tenant.Container.Kernel.Resolver.AddSubResolver(new ArrayResolver(tenant.Container.Kernel, true));
                tenant.Container.AddFacility<StartableFacility>();

                container.AddChildContainer(tenant.Container);
            }

            return manager;
        }

        private Boolean isStopped = false;
        public void Stop()
        {
            if (isStopped) return;
            var allShutDownActivities = _container.ResolveAll<IShutdownActivity>();
            foreach (var act in allShutDownActivities)
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

            //IMPORTANT: web application dispose WindsorContainer when disposed, so call 
            //to _webApplication.Dispose() should be done in the last call to stop.
            //IMPORTANT: disposing web application calls in DocumentBootstrapper for a second time.
            //need to check if the component was already stopped.
            isStopped = true;
            if (_webApplication != null)
            {
                _webApplication.Dispose();
            }
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
