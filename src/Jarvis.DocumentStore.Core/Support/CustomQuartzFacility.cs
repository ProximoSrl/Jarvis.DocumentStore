using System.Collections.Generic;
using Castle.Core.Logging;
using Castle.Facilities.QuartzIntegration;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;
using CQRS.Kernel.MultitenantSupport;
using Quartz;
using Quartz.Job;
using Quartz.Spi;

namespace Jarvis.DocumentStore.Core.Support
{
    public class CustomQuartzFacility : AbstractFacility
    {
        IDictionary<string,string> _configuration;
        private ILogger _logger;
        protected override void Init()
        {
            _logger = Kernel.Resolve<ILoggerFactory>().Create(GetType());

            AddComponent<FileScanJob>();
            AddComponent<IJobScheduler, QuartzNetSimpleScheduler>();
            AddComponent<IJobFactory, TenantJobFactory>();

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