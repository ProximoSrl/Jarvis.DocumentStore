using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Configuration;
using Castle.Facilities.QuartzIntegration;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Jarvis.ImageService.Core.Jobs;
using Jarvis.ImageService.Core.ProcessingPipeline;
using Jarvis.ImageService.Core.ProcessinPipeline;
using Quartz;
using Quartz.Impl.MongoDB;

namespace Jarvis.ImageService.Core.Support
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
                    .For<CreatePdfImageTask>()
                    .LifestyleTransient(),
                Component
                    .For<IPipelineScheduler>()
                    .ImplementedBy<PipelineScheduler>()
                    .LifestyleTransient()
            );

            var scheduler = container.Resolve<IScheduler>();
            ConfigureScheduler(scheduler);
        }

        static void ConfigureScheduler(IScheduler scheduler)
        {
            var job = JobBuilder
                .Create<HeartBeatJob>()
                .WithIdentity("HeartBeat")
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity("trigger-HeartBeat")
                .StartAt(DateTime.Now.AddSeconds(5))
                .WithSimpleSchedule(builder => builder.WithIntervalInSeconds(5).RepeatForever())
                .Build();

            scheduler.DeleteJob(JobKey.Create("HeartBeat"));
            scheduler.ScheduleJob(job, trigger);
        }

        IConfiguration CreateDefaultConfiguration()
        {
            var config = new MutableConfiguration("scheduler_config");
            var quartz = new MutableConfiguration("quartz");
            config.Children.Add(quartz);

            quartz.CreateChild("item", "Scheduler")
                    .Attribute("key", "quartz.scheduler.instanceName");

            quartz.CreateChild("item", "Quartz.Simpl.SimpleThreadPool, Quartz")
                    .Attribute("key", "quartz.threadPool.type");

            quartz.CreateChild("item", "5")
                    .Attribute("key", "quartz.threadPool.threadCount");

            quartz.CreateChild("item", "Normal")
                    .Attribute("key", "quartz.threadPool.threadPriority");

            quartz.CreateChild("item", "Quartz.Impl.MongoDB.JobStore, Quartz.Impl.MongoDB")
                    .Attribute("key", "quartz.jobStore.type");

            return config;
        }
    }
}