using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.Facilities.Startable;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Jarvis.DocumentStore.LiveBackup.Jobs;
using Jarvis.Framework.CommitBackup.Core;
using System;

namespace Jarvis.DocumentStore.LiveBackup.Support
{
    public class Bootstrapper
    {
        private IWindsorContainer _container;
        private ILogger _logger;

        public void Start(Boolean autoStartJobs = true)
        {
            _container = new WindsorContainer();
            if (autoStartJobs)
                _container.AddFacility<StartableFacility>();

            _container.AddFacility<LoggingFacility>(f =>
                f.LogUsing(LoggerImplementation.ExtendedLog4net)
                .WithConfig("log4net.config"));

            _logger = _container.Resolve<ILogger>();

            ConfigurationServiceSettingsConfiguration config = new ConfigurationServiceSettingsConfiguration();

            //Register only the configuration and the backup jobs.
            _container.Register(
                Component
                    .For<Configuration>()
                    .Instance(config),
                Component
                    .For<DocumentStoreBackupJob>()
           );
        }

        public void Stop()
        {
            _container.Dispose();
        }

        internal RestoreJob GetRestoreJob()
        {
            return _container.Resolve<RestoreJob>();
        }
    }
}
