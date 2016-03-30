using Castle.Core;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public class PollerManager  
    {
        readonly Dictionary<String, IPollerJobManager> _pollerJobManagers;

        private DocumentStoreConfiguration _configuration;

        private List<ClientInfo> queueClients;

        private class ClientInfo 
        {
            public ClientInfo(
                IPollerJobManager pollerManager,
                String queueName,
                String handle)
            {
                PollerManager = pollerManager;
                QueueName = queueName;
                Handle = handle;
            }

            public IPollerJobManager PollerManager { get; private set; }

            public String QueueName { get; private set; }

            public String Handle { get; private set; }
        }

        public ILogger Logger { get; set; }

        public PollerManager(
            IPollerJobManager[] pollerJobManagers,
            DocumentStoreConfiguration configuration)
        {
            _pollerJobManagers = pollerJobManagers.ToDictionary(p => p.GetType().Name, p => p);
            _configuration = configuration;
            queueClients = new List<ClientInfo>();
            Logger = NullLogger.Instance;
        }

        public void Start()
        {
            if (!_configuration.IsWorker) return; //I'm not a worker configuration.

            foreach (var queueInfo in _configuration.QueueInfoList.Where(info => info.PollersInfo != null))
            {
                //for each queue I can have more than one poller to observe
                foreach (var poller in queueInfo.PollersInfo)
                {
                    //for each queue I need to start client
                    if (!_pollerJobManagers.ContainsKey(poller.Name)) 
                    {
                        Logger.ErrorFormat("Unable to start polling with poller {0}, unknown poller class.");
                        continue;
                    }

                    string clientJobHandle = null;
                    try
                    {
                        clientJobHandle = _pollerJobManagers[poller.Name].Start(
                            queueInfo.Name,
                            poller.Parameters,
                            //@@TODO: allow multiple bindings to same service?
                            new List<String>() { _configuration.GetServerAddressForJobs() });
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorFormat(ex, "Exception launching job for queue {0}: {1}", queueInfo.Name, ex.Message);
                    }
                  
                    if (!String.IsNullOrEmpty(clientJobHandle))
                    {
                        queueClients.Add(new ClientInfo(_pollerJobManagers[poller.Name], queueInfo.Name, clientJobHandle));
                    }
                    else
                    {
                        Logger.ErrorFormat("Error starting client job for queue {0}", queueInfo.Name);
                    }
                }
               
            }
        }

        public void Stop()
        {
            if (_configuration.IsWorker == false) return;

            foreach (var queueInfo in queueClients.ToList())
            {
                queueInfo.PollerManager.Stop(queueInfo.Handle);
                queueClients.Remove(queueInfo);
            }
        }

        public void Restart(String queueId)
        {
            var client = queueClients.SingleOrDefault(c => c.QueueName == queueId);
            if (client != null)
            {
                client.PollerManager.Restart(client.Handle);
            }
        }
    }
    
   
}
