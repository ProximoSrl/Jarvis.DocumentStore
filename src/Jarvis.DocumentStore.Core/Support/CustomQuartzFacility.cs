using System;
using System.Collections.Generic;
using Castle.Facilities.QuartzIntegration;
using Castle.MicroKernel;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.Jobs;
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

    public class TenantJobFactory : IJobFactory
    {
        private readonly IKernel _kernel;

        /// <summary>
        /// Resolve a Job by it's name
        /// 
        /// </summary>
        public bool ResolveByJobName { get; set; }

        /// <summary>
        /// Creates a Quartz job with Windsor
        /// 
        /// </summary>
        /// <param name="kernel">Windsor Kernel</param>
        public TenantJobFactory(IKernel kernel)
        {
            this._kernel = kernel;
        }

        /// <summary>
        /// Called by the scheduler at the time of the trigger firing, in order to
        ///                         produce a <see cref="T:Quartz.IJob"/> instance on which to call Execute.
        /// 
        /// </summary>
        /// 
        /// <remarks>
        /// It should be extremely rare for this method to throw an exception -
        ///                         basically only the the case where there is no way at all to instantiate
        ///                         and prepare the Job for execution.  When the exception is thrown, the
        ///                         Scheduler will move all triggers associated with the Job into the
        ///                         <see cref="F:Quartz.TriggerState.Error"/> state, which will require human
        ///                         intervention (e.g. an application restart after fixing whatever
        ///                         configuration problem led to the issue wih instantiating the Job.
        /// 
        /// </remarks>
        /// <param name="bundle">The TriggerFiredBundle from which the <see cref="T:Quartz.IJobDetail"/>
        ///                           and other info relating to the trigger firing can be obtained.
        ///                         </param><param name="scheduler">a handle to the scheduler that is about to execute the job</param><throws>SchedulerException if there is a problem instantiating the Job. </throws>
        /// <returns>
        /// the newly instantiated Job
        /// 
        /// </returns>
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            if (bundle.JobDetail.JobDataMap.ContainsKey(JobKeys.TenantId))
            {
                TenantContext.Enter(new TenantId(bundle.JobDetail.JobDataMap.GetString(JobKeys.TenantId)));
            }

            return this.ResolveByJobName ? (IJob)this._kernel.Resolve(bundle.JobDetail.Key.ToString(), typeof(IJob)) : (IJob)this._kernel.Resolve(bundle.JobDetail.JobType);
        }

        public void ReturnJob(IJob job)
        {
            this._kernel.ReleaseComponent((object)job);
        }
    }
}