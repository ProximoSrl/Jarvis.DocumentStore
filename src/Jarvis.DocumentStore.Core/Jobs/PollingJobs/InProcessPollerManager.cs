using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Jobs.PollingJobs
{
    /// <summary>
    /// Starts all poller job in the same process as the caller.
    /// </summary>
    public class InProcessPollerJobManager : IPollerJobManager
    {
        readonly Dictionary<String, IPollerJob> _allPollerJobs;

        public InProcessPollerJobManager(IPollerJob[] allPollerJob)
        {
            _allPollerJobs = allPollerJob.Where(j => j.IsActive)
                .ToDictionary(j => j.QueueName, j => j);
        }

        public string Start(string queueId, List<string> docStoreAddresses)
        {
            if (!_allPollerJobs.ContainsKey(queueId)) return ""; //Job not started

            _allPollerJobs[queueId].Start();
            return queueId; //the id is the name of the job itself.
        }

        public bool Stop(string jobHandle)
        {
            _allPollerJobs[jobHandle].Stop();
            return true;
        }
    }
}
