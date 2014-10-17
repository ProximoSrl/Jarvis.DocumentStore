using System;
using System.Collections.Generic;
using Castle.Facilities.QuartzIntegration;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;
using Quartz;
using Quartz.Job;
using Quartz.Spi;

namespace Jarvis.DocumentStore.Core.Support
{
    public class CustomQuartzFacility : AbstractFacility
    {
        IDictionary<string,string> _configuration;

        protected override void Init()
        {
            AddComponent<FileScanJob>();
            AddComponent<IJobScheduler, QuartzNetSimpleScheduler>();
            AddComponent<IJobFactory, WindsorJobFactory>();

            var scheduler = new QuartzNetScheduler(
                _configuration,
                Kernel.Resolve<IJobFactory>(),
                Kernel
            );
            Kernel.Register(Component.For<IScheduler>().Instance(scheduler));
        }

        internal string AddComponent<T>()
        {
            string key = typeof (T).AssemblyQualifiedName;
            Kernel.Register(Component.For(typeof (T)).Named(key));
            return key;
        }

        internal string AddComponent<I, T>() where T : I
        {
            string key = typeof (T).AssemblyQualifiedName;
            Kernel.Register(Component.For(typeof (I)).ImplementedBy(typeof (T)).Named(key));
            return key;
        }

        public void Configure(IDictionary<string, string> configuration)
        {
            _configuration = configuration;
        }
    }
}