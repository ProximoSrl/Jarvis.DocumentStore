using System;
using System.Collections.Generic;
using Castle.Core.Logging;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Jarvis.DocumentStore.Core.Jobs;
using MongoDB.Driver;
using Quartz;

namespace Jarvis.DocumentStore.Core.Support
{
    public class SchedulerInstaller : IWindsorInstaller
    {
        readonly bool _autoStart;

        public SchedulerInstaller(bool autoStart)
        {
            _autoStart = autoStart;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.AddFacility<CustomQuartzFacility>(c => 
                c.Configure(CreateDefaultConfiguration())
            );

            container.Register(
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

            if (_autoStart)
                scheduler.Start();
        }

        IDictionary<string,string> CreateDefaultConfiguration()
        {
            var config = new Dictionary<string, string>();
            config["quartz.scheduler.instanceId"] = Environment.MachineName + "-" + DateTime.Now.ToShortTimeString();
            config["quartz.threadPool.type"] = "Quartz.Simpl.SimpleThreadPool, Quartz";
            config["quartz.threadPool.threadCount"] = (Environment.ProcessorCount *2).ToString();
            config["quartz.threadPool.threadPriority"] = "Normal";
            config["quartz.jobStore.type"] = "Quartz.Simpl.RAMJobStore, Quartz";
            return config;
        }
    }
}