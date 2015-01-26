using Castle.Core;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Jobs.PollingJobs
{
    public class PollerManager  
    {
        readonly IPollerJobManager _pollerJobManager;
        private DocumentStoreConfiguration _configuration;
        private Dictionary<String, String> queueClients;
        public ILogger Logger { get; set; }

        public PollerManager(
            IPollerJobManager pollerJobManager,
            DocumentStoreConfiguration configuration)
        {
            _pollerJobManager = pollerJobManager;
            _configuration = configuration;
            queueClients = new Dictionary<string, string>();
            Logger = NullLogger.Instance;
        }

        public void Start()
        {
            if (_configuration.JobMode != JobModes.Queue) return; //different job mode
            if (!_configuration.IsWorker) return; //I'm not a worker configuration.

            foreach (var queueInfo in _configuration.QueueInfoList)
            {
                //for each queue I need to start client
                var clientJobHandle = _pollerJobManager.Start(queueInfo.Name, new List<String>() {
                    _configuration.ServerAddress.AbsoluteUri
                });
                if (!String.IsNullOrEmpty(clientJobHandle))
                {
                    queueClients.Add(queueInfo.Name, clientJobHandle);
                }
                else
                {
                    Logger.ErrorFormat("Error starting client job for queue {0}", queueInfo.Name);
                }
            }
        }

        public void Stop()
        {
            if (_configuration.JobMode != JobModes.Queue) return; //different job mode
            if (_configuration.IsWorker == false) return;

            foreach (var queueInfo in queueClients.ToList())
            {
                _pollerJobManager.Stop(queueInfo.Value);
                queueClients.Remove(queueInfo.Key);
            }
        }
    }
    
   
}
