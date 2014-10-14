using Quartz;

namespace Jarvis.DocumentStore.Core.Support
{
    public class SchedulerShutdown : IShutdownActivity
    {
        readonly IScheduler _scheduler;

        public SchedulerShutdown(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public void Shutdown()
        {
            _scheduler.Shutdown();
        }
    }
}