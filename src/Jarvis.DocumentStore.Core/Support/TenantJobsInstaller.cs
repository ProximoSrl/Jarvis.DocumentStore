using System;
using Castle.Core.Logging;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.Jobs;
using Quartz;

namespace Jarvis.DocumentStore.Core.Support
{
    public class TenantJobsInstaller : IWindsorInstaller
    {
        private ITenant _tenant;

        public TenantJobsInstaller(ITenant tenant)
        {
            _tenant = tenant;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Classes
                    .FromAssemblyInThisApplication()
                    .BasedOn<ITenantJob>()
                    .WithServiceSelf()
                    .LifestyleTransient()
                );

            try
            {
                SetupCleanupJob(container.Resolve<IScheduler>());
            }
            catch (Exception ex)
            {
                container.Resolve<ILogger>().ErrorFormat(ex, "SetupCleanupJob");
            }
        }

        void SetupCleanupJob(IScheduler scheduler)
        {
            JobKey jobKey = JobKey.Create(_tenant.Id, "sys.cleanup");
            scheduler.DeleteJob(jobKey);

            var job = JobBuilder
                .Create<CleanupJob>()
                .UsingJobData(JobKeys.TenantId, _tenant.Id)
                .WithIdentity(jobKey)
                .Build();

            var trigger = TriggerBuilder.Create()
#if DEBUG
                .StartAt(DateTimeOffset.Now.AddSeconds(30))
                .WithSimpleSchedule(b => b.RepeatForever().WithIntervalInSeconds(15))
#else
                .StartAt(DateTimeOffset.Now.AddMinutes(5))
                .WithSimpleSchedule(b=>b.RepeatForever().WithIntervalInMinutes(5))
#endif
.WithPriority(1)
                .Build();

            scheduler.ScheduleJob(job, trigger);
        }
    }
}