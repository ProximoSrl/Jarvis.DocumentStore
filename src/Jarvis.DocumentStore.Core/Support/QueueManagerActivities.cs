
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Quartz;

namespace Jarvis.DocumentStore.Core.Support
{
    public class QueueManagerStartupActivity : IStartupActivity
    {
        readonly IQueueDispatcher _queueManager;
        readonly DocumentStoreConfiguration _config;
        readonly PollerManager _pollerManager;

        public QueueManagerStartupActivity(QueueManager queueManager, DocumentStoreConfiguration config, PollerManager pollerManager)
        {
            _queueManager = queueManager;
            _config = config;
            _pollerManager = pollerManager;
        }

        public void Start()
        {
            if (_config.IsQueueManager)
                _queueManager.Start();

            if (_config.JobMode == JobModes.Queue)
                _pollerManager.Start();
        }
    }

    public class QueueManagerShutdownActivity : IShutdownActivity
    {
        readonly IQueueDispatcher _queueManager;
        readonly DocumentStoreConfiguration _config;
        readonly PollerManager _pollerManager;

        public QueueManagerShutdownActivity(QueueManager queueManager, DocumentStoreConfiguration config, PollerManager pollerManager)
        {
            _queueManager = queueManager;
            _config = config;
            _pollerManager = pollerManager;
        }

        public void Shutdown()
        {
            if (_config.IsQueueManager)
                _queueManager.Stop();

            if (_config.JobMode == JobModes.Queue)
                _pollerManager.Stop();
        }
    }
}