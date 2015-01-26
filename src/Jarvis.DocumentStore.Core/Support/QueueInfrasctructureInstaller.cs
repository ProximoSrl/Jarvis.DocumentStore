using System;
using System.Collections.Generic;
using System.Threading;
using Castle.Core.Configuration;
using Castle.Core.Logging;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Jobs.OutOfProcessPollingJobs;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Processing.Pipeline;
using MongoDB.Driver;
using Quartz;
using Quartz.Impl.MongoDB;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Jarvis.DocumentStore.Core.Jobs.PollingJobs;

namespace Jarvis.DocumentStore.Core.Support
{
    public class QueueInfrasctructureInstaller : IWindsorInstaller
    {
        private String _queueStoreConnectionString;
        private IEnumerable<QueueInfo> _queuesInfo;

        public QueueInfrasctructureInstaller(string queueStoreConnectionString, IEnumerable<QueueInfo> queuesInfo)
        {
            _queueStoreConnectionString = queueStoreConnectionString;
            _queuesInfo = queuesInfo;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            var queueDb = GetQueueDb();
            container.Register(
                Component
                    .For<QueueManager, IQueueDispatcher>()
                    .ImplementedBy<QueueManager>()
                    .DependsOn(Dependency.OnValue<MongoDatabase>(queueDb))
                    .LifeStyle.Singleton,
                Classes.FromAssemblyInThisApplication()
                    .BasedOn<IPollerJob>()
                    .WithServiceFirstInterface(),
                //Component
                //    .For<IPollerJobManager>()
                //    .ImplementedBy<InProcessPollerJobManager>(),
               Component
                    .For<IPollerJobManager>()
                    .ImplementedBy<OutOfProcessBaseJobManager>(),
                Component
                    .For<PollerManager>()
                    .ImplementedBy<PollerManager>()
            );

            //now register all handlers
            foreach (var queueInfo in _queuesInfo)
            {
                container.Register(Component.For<QueueHandler>()
                    .ImplementedBy<QueueHandler>()
                    .DependsOn(Dependency.OnValue<QueueInfo>(queueInfo))
                    .DependsOn(Dependency.OnValue<MongoDatabase>(queueDb))
                    .Named("QueueHandler-" + queueInfo.Name));
            }
        }

        MongoDatabase GetQueueDb()
        {
            var url = new MongoUrl(_queueStoreConnectionString);
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