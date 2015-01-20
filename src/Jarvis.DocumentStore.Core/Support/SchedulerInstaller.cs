using System;
using System.Collections.Generic;
using System.Threading;
using Castle.Core.Configuration;
using Castle.Core.Logging;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Processing.Pipeline;
using MongoDB.Driver;
using Quartz;
using Quartz.Impl.MongoDB;

namespace Jarvis.DocumentStore.Core.Support
{
    public class SchedulerInstaller : IWindsorInstaller
    {
        readonly bool _autoStart;

        public SchedulerInstaller(string jobStoreConnectionString, bool autoStart)
        {
            _autoStart = autoStart;
            JobStore.DefaultConnectionString = jobStoreConnectionString;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.AddFacility<CustomQuartzFacility>(c => 
                c.Configure(CreateDefaultConfiguration())
            );

            container.Register(
                Component
                    .For<IJobHelper>()
                    .ImplementedBy<JobHelper>(),
                Classes
                    .FromAssemblyInThisApplication()
                    .BasedOn<IPipeline>()
                    .WithServiceFirstInterface(),
                Classes
                    .FromAssemblyInThisApplication()
                    .BasedOn<IPipelineListener>()
                    .WithServiceFirstInterface(),
                Classes
                    .FromAssemblyInThisApplication()
                    .BasedOn<IShutdownActivity>()
                    .WithServiceFirstInterface(),
                Classes
                    .FromAssemblyInThisApplication()
                    .BasedOn<IStartupActivity>()
                    .WithServiceFirstInterface()
            );

            var scheduler = container.Resolve<IScheduler>();

            MongoDatabase quartzDb = GetSchedulerDb();
            scheduler.ListenerManager.AddJobListener(new JobsListener(
                container.Resolve<IExtendedLogger>(),
                quartzDb
            ));

            container.Register(
                Component
                    .For<JobStats>()
                    .DependsOn(Dependency.OnValue<MongoDatabase>(quartzDb))
            );

            if (_autoStart)
                scheduler.Start();
        }

        MongoDatabase GetSchedulerDb()
        {
            var url = new MongoUrl(JobStore.DefaultConnectionString);
            var client = new MongoClient(url);
            return client.GetServer().GetDatabase(url.DatabaseName);
        }

        IDictionary<string,string> CreateDefaultConfiguration()
        {
            var config = new Dictionary<string, string>();
            config["quartz.scheduler.instanceName"] = QuartzMongoConfiguration.Name;
            config["quartz.scheduler.instanceId"] = Environment.MachineName + "-" + DateTime.Now.ToShortTimeString();
            config["quartz.threadPool.type"] = "Quartz.Simpl.SimpleThreadPool, Quartz";
            config["quartz.threadPool.threadCount"] = (Environment.ProcessorCount *2).ToString();
            config["quartz.threadPool.threadPriority"] = "Normal";
            config["quartz.jobStore.type"] = "Quartz.Impl.MongoDB.JobStore, Quartz.Impl.MongoDB";
            return config;
        }
    }
}