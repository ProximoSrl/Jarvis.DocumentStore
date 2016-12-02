using Castle.Facilities.Logging;
using Castle.Facilities.Startable;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Jarvis.DocumentStore.LiveBackup.Jobs;
using Jarvis.Framework.CommitBackup.Core;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.LiveBackup.Support
{
    public class Bootstrapper
    {
        IWindsorContainer _container;

        public void Start(Boolean autoStartJobs = true)
        {
            _container = new WindsorContainer();
            if (autoStartJobs)
                _container.AddFacility<StartableFacility>();

            _container.AddFacility<LoggingFacility>(f =>
                f.LogUsing(LoggerImplementation.ExtendedLog4net)
                .WithConfig("log4net.config"));

            ConfigurationServiceSettingsConfiguration config = new ConfigurationServiceSettingsConfiguration();

            _container.Register(
                Component.For<Func<IMongoDatabase, ICommitReader>>()
                    .Instance(d => new PlainCommitMongoReader(d)),
                Component.For<Func<String, Int64, ICommitWriter>>()
                    .Instance((directory, fileSize) => new PlainTextFileCommitWriter(directory, fileSize)),
                Component
                    .For<Configuration>()
                    .Instance(config),
                Component
                    .For<BackupJob>(),
                Component
                    .For<RestoreJob>()
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
