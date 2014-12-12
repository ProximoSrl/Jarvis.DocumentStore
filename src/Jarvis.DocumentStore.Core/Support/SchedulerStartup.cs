using Quartz;

namespace Jarvis.DocumentStore.Core.Support
{
    public class SchedulerStartup : IStartupActivity
    {
        readonly IScheduler _scheduler;
        readonly DocumentStoreConfiguration _config;

        public SchedulerStartup(IScheduler scheduler, DocumentStoreConfiguration config)
        {
            _scheduler = scheduler;
            _config = config;
        }

        public void Start()
        {
            if (_config.IsWorker)
                _scheduler.Start();

        }
    }
}