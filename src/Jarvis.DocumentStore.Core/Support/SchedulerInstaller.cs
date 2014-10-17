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
using Jarvis.DocumentStore.Core.Processing.Pdf;
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

            scheduler.ListenerManager.AddJobListener(new JobsListener(
                container.Resolve<IExtendedLogger>(),
                container.Resolve<MongoDatabase>()
            ));

            SetupCleanupJob(scheduler);

            if (_autoStart)
                scheduler.Start();
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

        IDictionary<string,string> CreateDefaultConfiguration()
        {
            var config = new Dictionary<string, string>();
            config["quartz.scheduler.instanceName"] = "jarvis.documentstore";
            config["quartz.scheduler.instanceId"] = Environment.MachineName + "-" + DateTime.Now.ToShortTimeString();
            config["quartz.threadPool.type"] = "Quartz.Simpl.SimpleThreadPool, Quartz";
            config["quartz.threadPool.threadCount"] = Environment.ProcessorCount.ToString();
            config["quartz.threadPool.threadPriority"] = "Normal";
            config["quartz.jobStore.type"] = "Quartz.Impl.MongoDB.JobStore, Quartz.Impl.MongoDB";
            return config;
        }
    }
}