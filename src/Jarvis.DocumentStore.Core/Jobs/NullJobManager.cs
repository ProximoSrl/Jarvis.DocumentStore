using System.Collections.Generic;

namespace Jarvis.DocumentStore.Core.Jobs
{
    /// <summary>
    /// <para>
    /// A queue whit this job manager is completely not managed, this
    /// is the standard situation when poller are managed manually in other
    /// machines and should not be managed by DocumentPRocess.
    /// </para>
    /// <para>
    /// This is needed because there are situation where I want to configure
    /// a queue with workers that are managed outside document store.
    /// </para>
    /// </summary>
    public class NullJobManager : IPollerJobManager
    {
        public List<PollingJobInfo> GetAllJobsInfo()
        {
            return new List<PollingJobInfo>();
        }

        public bool RestartWorker(string jobHandle, bool forceClose)
        {
            return true;
        }

        public string Start(string queueName, Dictionary<string, string> customParameters, List<string> docStoreAddresses)
        {
            return "NULLEXECUTOR:" + queueName;
        }

        public bool Stop(string jobHandle)
        {
            return true;
        }

        public bool SuspendWorker(string handle)
        {
            return true;
        }
    }
}
