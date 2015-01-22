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
        public string Start(string queueId, List<string> docStoreAddresses)
        {
            throw new NotImplementedException();
        }

        public bool Stop(string jobHandle)
        {
            throw new NotImplementedException();
        }
    }
}
