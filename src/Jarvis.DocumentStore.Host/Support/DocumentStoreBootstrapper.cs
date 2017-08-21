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
using Metrics;
using Jarvis.Framework.Kernel.ProjectionEngine.Client;
using Jarvis.DocumentStore.Host.Controllers;
using System.Threading;
using MongoDB.Driver;
using Jarvis.Framework.Kernel.Support;
using Castle.Windsor.Diagnostics;
using Castle.MicroKernel;
using Jarvis.NEventStoreEx.CommonDomainEx.Persistence;
using Jarvis.NEventStoreEx;
using Jarvis.Framework.Shared;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Clusters;
using Jarvis.DocumentStore.Shared.Helpers;
using Jarvis.Framework.Shared.Helpers;

namespace Jarvis.DocumentStore.Host.Support
{
    public class DocumentStoreBootstrapper
    {
        IDisposable _webApplication;
        IWindsorContainer _container;
        ILogger _logger;
        DocumentStoreConfiguration _config;
        private Boolean _initialized = false;

        private Boolean isStopped = false;
        public TenantManager Manager { get; private set; }

        private String[] _databaseNames = new[] { "events", "originals", "artifacts", "system", "readmodel" };

        public void Start(DocumentStoreConfiguration config)
        {
            _config = config;
            BuildContainer(config);

            if (_config.EnableSingleAggregateRepositoryCache)
            {
                _logger.InfoFormat("Single Aggregate Repository Cache - ENABLED");
                JarvisFrameworkGlobalConfiguration.EnableSingleAggregateRepositoryCache();
            }
            else
            {
                _logger.InfoFormat("Single Aggregate Repository Cache - DISABLED");
                JarvisFrameworkGlobalConfiguration.DisableSingleAggregateRepositoryCache();
            }
            if (_config.DisableRepositoryLockOnAggregateId)
            {
                _logger.InfoFormat("Repository lock on Aggregate Id - DISABLED");
                NeventStoreExGlobalConfiguration.DisableRepositoryLockOnAggregateId();
            }
            else
            {
                _logger.InfoFormat("Repository lock on Aggregate Id - ENABLED");
                NeventStoreExGlobalConfiguration.EnableRepositoryLockOnAggregateId();
            }

            Manager = BuildTenants(_container, config);
            //Setup database check.
            foreach (var tenant in _config.TenantSettings)
            {
                foreach (var connection in _databaseNames)
                {
                    DatabaseHealthCheck check = new DatabaseHealthCheck(
                          String.Format("Tenant: {0} [Db:{1}]", tenant.TenantId, connection),
                          tenant.GetConnectionString(connection));
                }
            }

            while (StartupCheck() == false)
            {
                _logger.InfoFormat("Some precondition to start the service are not met. Will retry in 3 seconds!");
                Thread.Sleep(3000);
            }

            if (RebuildSettings.ShouldRebuild && Environment.UserInteractive)
            {
                Console.WriteLine("---> Set Log Level to INFO to speedup rebuild (y/N)?");
                var res = Console.ReadLine().Trim().ToLowerInvariant();
                if (res == "y")
                {
                    SetLogLevelTo("INFO");
                }
            }

            _logger.DebugFormat(
                "Roles:\n  api: {0}\n  worker : {1}\n  projections: {2}",
                config.IsApiServer,
                config.IsWorker,
                config.IsReadmodelBuilder
            );

            InitializeEverything(config);

            //Check if container misconfigured
            _container.CheckConfiguration();          
        }

        private bool StartupCheck()
        {
            var result = CheckDatabase();

            if (!result)
                _logger.Warn("One or more mongo instances are not operational.");

            return result;
        }

        private bool CheckDatabase()
        {
            //verify all connection to MongoDB. Lots of object initialize everything in constructor and initialization
            //fails if mongo database is not operational.
            if (!CheckConnection(_config.QueueConnectionString))
                return false;

            foreach (var tenant in _config.TenantSettings)
            {
                foreach (var connection in _databaseNames)
                {
                    if (!CheckConnection(tenant.GetConnectionString(connection)))
                        return false;
                }
            }
            return true;
        }

        private Boolean CheckConnection(String connection)
        {
            var url = new MongoUrl(connection);
            var client = new MongoClient(url);
            Task.Factory.StartNew(() =>
            {
                var allDb = client.ListDatabases();
            }); //forces a database connection
            Int32 spinCount = 0;
            ClusterState clusterState;

            while ((clusterState = client.Cluster.Description.State) != ClusterState.Connected &&
                spinCount++ < 100)
            {
                Thread.Sleep(20);
            }
            return clusterState == MongoDB.Driver.Core.Clusters.ClusterState.Connected;
        }

        private void InitializeEverything(DocumentStoreConfiguration config)
        {
            var installers = new List<IWindsorInstaller>()
            {
                new CoreInstaller(config),
                new EventStoreInstaller(Manager, config),
                new SchedulerInstaller(false),
                new BackgroundTasksInstaller(config),
                new QueueInfrasctructureInstaller(config.QueueConnectionString, config.QueueInfoList),
            };

            _logger.Debug("Configured Scheduler");

            if (config.HasMetersEnabled)
            {
                //@@TODO: https://github.com/etishor/Metrics.NET/wiki/ElasticSearch
                var binding = config.MetersOptions["http-endpoint"];
                _logger.DebugFormat("Meters available on {0}", binding);

                Metric
                    .Config
                    .WithHttpEndpoint(binding)
                    .WithAllCounters();
            }

            DocumentStoreApplication.SetConfig(config);
            if (config.IsApiServer)
            {
                installers.Add(new ApiInstaller());
            }

            var options = new StartOptions();
            foreach (var uri in config.ServerAddresses)
            {
                _logger.InfoFormat("Binding to @ {0}", uri);
                options.Urls.Add(uri);
            }

            _container.Install(installers.ToArray());
            foreach (var tenant in Manager.Tenants)
            {
                var tenantInstallers = new List<IWindsorInstaller>
                {
                    new TenantCoreInstaller(tenant),
                    new TenantHandlersInstaller(tenant),
                    new TenantJobsInstaller(tenant)
                };

                if (config.IsApiServer)
                {
                    tenantInstallers.Add(new TenantApiInstaller());
                }

                tenantInstallers.Add(new TenantProjectionsInstaller<NotifyReadModelChanges>(tenant, config));
                _logger.DebugFormat("Configured Projections for tenant {0}", tenant.Id);

                tenant.Container.Install(tenantInstallers.ToArray());
                tenant.Container.CheckConfiguration();

            }

            _webApplication = WebApp.Start<DocumentStoreApplication>(options);
            _logger.InfoFormat("Server started");

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
            _initialized = true;
        }

        void BuildContainer(DocumentStoreConfiguration config)
        {
            _container = new WindsorContainer();
            ContainerAccessor.Instance = _container;
            _container.Register(Component.For<DocumentStoreConfiguration>().Instance(config));
            _container.Kernel.Resolver.AddSubResolver(new CollectionResolver(_container.Kernel, true));
            _container.Kernel.Resolver.AddSubResolver(new ArrayResolver(_container.Kernel, true));

            _container.AddFacility<LoggingFacility>(config.CreateLoggingFacility);

#if DEBUG
            UdpAppender.AppendToConfiguration();
#endif

            _container.AddFacility<StartableFacility>();
            _container.AddFacility<JarvisTypedFactoryFacility>();

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
                tenant.Container.AddFacility<JarvisTypedFactoryFacility>();
                container.AddChildContainer(tenant.Container);
            }

            return manager;
        }

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
            if (!_config.IsApiServer)
            {
                _container.Dispose();
            }
        }

        /// <summary>
        /// http://forums.asp.net/t/1969159.aspx?How+to+change+log+level+for+log4net+from+code+behind+during+the+application+up+and+running+
        /// </summary>
        /// <param name="logLevel"></param>
        private void SetLogLevelTo(string logLevel)
        {
            try
            {
                log4net.Repository.ILoggerRepository[] repositories = log4net.LogManager.GetAllRepositories();

                //Configure all loggers to be at the debug level.
                foreach (log4net.Repository.ILoggerRepository repository in repositories)
                {
                    repository.Threshold = repository.LevelMap[logLevel];
                    log4net.Repository.Hierarchy.Hierarchy hier = (log4net.Repository.Hierarchy.Hierarchy)repository;
                    log4net.Core.ILogger[] loggers = hier.GetCurrentLoggers();
                    foreach (log4net.Core.ILogger logger in loggers)
                    {
                        ((log4net.Repository.Hierarchy.Logger)logger).Level = hier.LevelMap[logLevel];
                    }
                }

                //Configure the root logger.
                log4net.Repository.Hierarchy.Hierarchy h = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
                log4net.Repository.Hierarchy.Logger rootLogger = h.Root;
                rootLogger.Level = h.LevelMap[logLevel];
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR CHANGING LOG LEVEL {0}", ex.Message);
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
