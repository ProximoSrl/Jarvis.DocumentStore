using Castle.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Jobs.PollingJobs
{
    /// <summary>
    /// Starts all poller job in the same process as the caller. This is a
    /// obsolete class because we prefer using the <see cref="OutOfProcessBaseJobManager" />
    /// class because is more resilient to errors.
    /// </summary>
    public class InProcessPollerJobManager : IPollerJobManager
    {
        /// <summary>
        /// Dictionary of jobs, the key is the queueId and the value
        /// is a job that was designed to run in process.
        /// </summary>
        readonly Dictionary<String, IPollerJob> _allPollerJobs;

        public ILogger Logger { get; set; }

        public InProcessPollerJobManager(IPollerJob[] allPollerJob)
        {
            _allPollerJobs = allPollerJob
                .Where(j => j.IsActive && j.IsOutOfProcess == false)
                .ToDictionary(j => j.QueueName, j => j);
            Logger = NullLogger.Instance;
        }

        public string Start(string queueId, List<string> docStoreAddresses)
        {
            if (!_allPollerJobs.ContainsKey(queueId)) return ""; //Job not started

            _allPollerJobs[queueId].Start(new List<String>(), queueId); //in process poller does not need addresses
            return queueId; //the id is the name of the job itself.
        }

        public bool Stop(string jobHandle)
        {
            _allPollerJobs[jobHandle].Stop();
            return true;
        }

        public bool Restart(string jobHandle)
        {
            if (!Stop(jobHandle))
            {
                Logger.ErrorFormat("Unable to stop job with handle {0}", jobHandle);
                return false;
            }
            Start(jobHandle, new List<String>());
            return true;
        }
    }
}
