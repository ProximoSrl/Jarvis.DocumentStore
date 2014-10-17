using System;
using System.Threading;
using Castle.Core.Configuration;
using Castle.Core.Logging;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Processing.Pdf;
using Jarvis.DocumentStore.Core.Processing.Pipeline;
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
                    .For<IPipelineManager>()
                    .ImplementedBy<PipelineManager>(),
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
                Component
                    .For<IShutdownActivity>()
                    .ImplementedBy<SchedulerShutdown>()
                    .Named("SchedulerShoutdown")
            );

            var scheduler = container.Resolve<IScheduler>();
//            scheduler.PauseAll();

            scheduler.ListenerManager.AddJobListener(new JobsListener(
                container.Resolve<IExtendedLogger>(),
                container.Resolve<MongoDatabase>()
            ));

            SetupCleanupJob(scheduler);
        }

        void SetupCleanupJob(IScheduler scheduler)
        {
            scheduler.DeleteJob(JobKey.Create("sys.cleanup"));

            var job = JobBuilder
                .Create<CleanupJob>()
                .WithIdentity("sys.cleanup")
                .Build();

            var trigger = TriggerBuilder.Create()
                .StartAt(DateTimeOffset.Now)
#if DEBUG
                .WithSimpleSchedule(b => b.RepeatForever().WithIntervalInSeconds(15))
#else
                .WithSimpleSchedule(b=>b.RepeatForever().WithIntervalInMinutes(5))
#endif
                .WithPriority(1)
                .Build();

            scheduler.ScheduleJob(job, trigger);
        }

        IConfiguration CreateDefaultConfiguration()
        {
            var config = new MutableConfiguration("scheduler_config");
            var quartz = new MutableConfiguration("quartz");
            config.Children.Add(quartz);

            quartz.CreateChild("item", "jarvis.documentstore")
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