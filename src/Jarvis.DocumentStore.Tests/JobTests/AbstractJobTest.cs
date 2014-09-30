using System.Collections.Generic;
using System.Linq;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Calendar;
using Quartz.Impl.Triggers;
using Quartz.Spi;

namespace Jarvis.DocumentStore.Tests.JobTests
{
    public abstract class AbstractJobTest
    {
        protected IJobExecutionContext BuildContext(IJob job, IEnumerable<KeyValuePair<string, object>> map = null)
        {
            var scheduler = NSubstitute.Substitute.For<IScheduler>();
            var firedBundle = new TriggerFiredBundle(
                new JobDetailImpl("job", job.GetType()),
                new SimpleTriggerImpl("trigger"),
                new AnnualCalendar(),
                false,
                null, null, null, null
                );

            if (map != null)
            {
                foreach (var kvp in map)
                {
                    firedBundle.JobDetail.JobDataMap.Add(kvp);
                }
            }

            return new JobExecutionContextImpl(scheduler, firedBundle, job);
        }

        protected IJobExecutionContext BuildContext(IJob job, object keyValMap)
        {
            var dictionary = keyValMap.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(keyValMap));
            return BuildContext(job, dictionary);
        }
    }
}