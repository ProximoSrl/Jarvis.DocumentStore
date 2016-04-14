using Metrics;
using Metrics.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Support
{
    public class MetricHeartBeatHealthCheck : HealthCheck
    {

        public static MetricHeartBeatHealthCheck Create(
            String description, 
            Int32 timeoutInMilliseconds,
            TimeSpan startOffset)
        {
            return new MetricHeartBeatHealthCheck(description, timeoutInMilliseconds, startOffset);
        }

        private Int32 _timeoutInMilliseconds;

        protected MetricHeartBeatHealthCheck(String description, Int32 timeoutInMilliseconds, TimeSpan startOffset)
            : base(description)
        {
            HealthChecks.RegisterHealthCheck(this);
            _timeoutInMilliseconds = timeoutInMilliseconds;
            _lastActivityTickCount = Environment.TickCount - (Int64) startOffset.TotalMilliseconds;
        }

        private Int64 _lastActivityTickCount;

        public void Pulse()
        {
            _lastActivityTickCount = Environment.TickCount;
        }

        protected override HealthCheckResult Check()
        {
            var elapsed = Math.Abs(Environment.TickCount - _lastActivityTickCount);
            if (elapsed > _timeoutInMilliseconds)
            {
                return HealthCheckResult.Unhealthy(String.Format("Last HeartBeat {0} ms ago", elapsed));
            }
            return HealthCheckResult.Healthy();
        }
    }

}
