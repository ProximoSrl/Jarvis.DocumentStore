using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Jobs.PollingJobs
{
    public interface IPollerJobManager
    {
        /// <summary>
        /// Starts a new poller job.
        /// </summary>
        /// <param name="docStoreAddresses"></param>
        /// <param name="queueId">The id of the queue associated with the job.</param>
        /// <returns>An opaque handle that represent the new identity of the poller.</returns>
        String Start(String queueId, List<String> docStoreAddresses);

        /// <summary>
        /// Stops poller job, 
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        Boolean Stop(String jobHandle);
    }

    public interface IPollerJob 
    {
        String QueueName { get; }

        Boolean IsActive { get; }

        /// <summary>
        /// Tells me if the job can execute only in process or is made to be executed
        /// in out of process.
        /// </summary>
        Boolean IsOutOfProcess { get;  }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="documentStoreAddressUrls">List of document store addressess</param>
        /// <param name="handle">The handle used to specify the identity of the worker.</param>
        void Start(List<String> documentStoreAddressUrls, String handle);

        void Stop();
    }
}
