using System;
using Castle.Core.Logging;
using Castle.MicroKernel;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Shared.Jobs;
using Jarvis.Framework.Shared.MultitenantSupport;
using Quartz;
using Quartz.Spi;

namespace Jarvis.DocumentStore.Core.Support
{
    public class TenantJobFactory : IJobFactory
    {
        private readonly IKernel _kernel;
        ITenantAccessor _tenantAccessor;
        public ILogger Logger { get; set; }
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
            var jobType = bundle.JobDetail.JobType.FullName;
            TenantId tenantId = null;
            if (bundle.JobDetail.JobDataMap.ContainsKey(JobKeys.TenantId))
            {
                tenantId = new TenantId(
                    bundle.JobDetail.JobDataMap.GetString(JobKeys.TenantId)
                    );
                Logger.DebugFormat("new job {0} on tenant {1}", jobType, tenantId );
            }
            else
            {
                if (typeof (ITenantJob).IsAssignableFrom(bundle.JobDetail.JobType))
                {
                    string message = String.Format("Job {0}: missing tenantId", jobType);
                    Logger.Error(message);
                    throw new Exception(message);
                }

                Logger.DebugFormat("new job {0} without tenant", jobType);
            }

            var kernel = SelectKernel(tenantId);
            var job = this.ResolveByJobName ? 
                (IJob)kernel.Resolve(bundle.JobDetail.Key.ToString(), typeof(IJob)) :
                (IJob)kernel.Resolve(bundle.JobDetail.JobType);

            if (job is ITenantJob)
            {
                (job as ITenantJob).TenantId = new TenantId(tenantId);
            }

            return job;
        }

        public void ReturnJob(IJob job)
        {
            TenantId tenantId = null;
            if (job is ITenantJob)
            {
                tenantId = (job as ITenantJob).TenantId;
            }

            SelectKernel(tenantId).ReleaseComponent((object)job);
        }

        IKernel SelectKernel(TenantId tenantId)
        {
            if (tenantId == null)
                return _kernel;

            if (_tenantAccessor == null)
            {
                // concurrency safe, singleton
                _tenantAccessor = _kernel.Resolve<ITenantAccessor>();
            }

            var tenant = _tenantAccessor.GetTenant(new TenantId(tenantId));
            return tenant.Container.Kernel;
        }
    }
}