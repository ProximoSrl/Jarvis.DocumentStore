using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Quartz;

namespace Jarvis.DocumentStore.Core.Support
{
    public class QueueManagerStartup : IStartupActivity
    {
        readonly QueueManager _queueManager;
        readonly DocumentStoreConfiguration _config;

        public QueueManagerStartup(QueueManager queueManager, DocumentStoreConfiguration config)
        {
            _queueManager = queueManager;
            _config = config;
        }

        public void Start()
        {
            if (_config.IsQueueManager)
                _queueManager.Start();

        }
    }
}