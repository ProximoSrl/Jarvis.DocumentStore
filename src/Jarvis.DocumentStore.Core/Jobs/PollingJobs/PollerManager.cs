using Castle.Core;
using Jarvis.DocumentStore.Core.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Jobs.PollingJobs
{
    public class PollerManager : IStartable 
    {
        readonly IPollerJobManager _pollerJobManager;
        private DocumentStoreConfiguration _configuration;

        public PollerManager(
            IPollerJobManager pollerJobManager,
            DocumentStoreConfiguration configuration)
        {
            _pollerJobManager = pollerJobManager;
            _configuration = configuration;
        }

        public void Start()
        {
            if (_configuration.JobMode != JobModes.Queue) return; //different job mode
            if (!_configuration.IsWorker) return; //I'm not a worker configuration.

            foreach (var queueInfo in _configuration.QueueInfoList)
            {
                //for each queue I need to start client
                _pollerJobManager.Start(queueInfo.Name, new List<String>()); // actually we still work with command queue.
            }
        }

        public void Stop()
        {
            if (_configuration.JobMode != JobModes.Queue) return; //different job mode
            if (_configuration.IsWorker == false) return;

            foreach (var queueInfo in _configuration.QueueInfoList)
            {
                _pollerJobManager.Stop(queueInfo.Name); 
            }
        }
    }
    
   
}
