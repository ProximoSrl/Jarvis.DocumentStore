using System;
using Castle.Core.Configuration;
using Castle.Core.Logging;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.ProcessingPipeline;
using Jarvis.DocumentStore.Core.ProcessingPipeline.Pdf;
using MongoDB.Driver;
using Quartz;
using Quartz.Impl.MongoDB;

namespace Jarvis.DocumentStore.Core.Support
{
    public class SchedulerInstaller : IWindsorInstaller
    {
        public SchedulerInstaller(string jobStoreConnectionString)
        {
            JobStore.DefaultConnectionString = jobStoreConnectionString;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.AddFacility<CustomQuartzFacility>(c => c.Configure(CreateDefaultConfiguration()));

            container.Register(
                Classes
                    .FromAssemblyInThisApplication()
                    .BasedOn<IJob>()
                    .WithServiceSelf()
                    .LifestyleTransient(),
                Component
                    .For<CreateImageFromPdfTask>()
                    .LifestyleTransient(),
                Component
                    .For<IConversionWorkflow>()
                    .ImplementedBy<ConversionWorkflow>()
            );

            container.Resolve<IScheduler>().ListenerManager.AddJobListener(new JobsListener(
                container.Resolve<ILogger>(),
                container.Resolve<IConversionWorkflow>(),
                container.Resolve<MongoDatabase>()
            ));
        }

        IConfiguration CreateDefaultConfiguration()
        {
            var config = new MutableConfiguration("scheduler_config");
            var quartz = new MutableConfiguration("quartz");
            config.Children.Add(quartz);

            quartz.CreateChild("item", "jarvis")
                    .Attribute("key", "quartz.scheduler.instanceName");

            quartz.CreateChild("item", Environment.MachineName + "-"+DateTime.Now.ToShortTimeString())
                    .Attribute("key", "quartz.scheduler.instanceId");

            quartz.CreateChild("item", "Quartz.Simpl.SimpleThreadPool, Quartz")
                    .Attribute("key", "quartz.threadPool.type");

            quartz.CreateChild("item", Environment.ProcessorCount.ToString())
                    .Attribute("key", "quartz.threadPool.threadCount");

            quartz.CreateChild("item", "Normal")
                    .Attribute("key", "quartz.threadPool.threadPriority");

            quartz.CreateChild("item", "Quartz.Impl.MongoDB.JobStore, Quartz.Impl.MongoDB")
                    .Attribute("key", "quartz.jobStore.type");

            return config;
        }
    }
}