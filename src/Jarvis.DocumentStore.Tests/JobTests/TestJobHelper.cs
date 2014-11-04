using System.Collections.Generic;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Calendar;
using Quartz.Impl.Triggers;
using Quartz.Spi;

namespace Jarvis.DocumentStore.Tests.JobTests
{
    public class TestJobHelper
    {
        public static IJobExecutionContext BuildContext(IJob job, IEnumerable<KeyValuePair<string, object>> map = null)
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
    }
}