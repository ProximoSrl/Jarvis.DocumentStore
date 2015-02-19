using System;
using System.Collections.Generic;

namespace Jarvis.DocumentStore.Core.Jobs
{
    /// <summary>
    /// Manage start and stop of jobs based on poller queue.
    /// </summary>
    public interface IPollerJobManager
    {
        /// <summary>
        /// Starts a new poller job.
        /// </summary>
        /// <param name="docStoreAddresses"></param>
        /// <param name="queueId">The info of the queue associated with the job.</param>
        /// <returns>An opaque handle that represent the new identity of the poller.</returns>
        String Start(String queueName, Dictionary<String, String> customParameters, List<String> docStoreAddresses);

        /// <summary>
        /// Stops poller job, 
        /// </summary>
        /// <param name="jobHandle">Handle of the job to stop. This is the handle returned from
        /// a call to the <see cref="Start" /> method</param>
        /// <returns></returns>
        Boolean Stop(String jobHandle);

        /// <summary>
        /// If for some reason a job is blocked (es a job is in executing state for more than 
        /// a certain amount of time) this method is used to reset and restart the job.
        /// </summary>
        /// <param name="jobHandle">Handle of the job to stop. This is the handle returned from
        /// a call to the <see cref="Start" /> method</param>
        /// <returns></returns>
        Boolean Restart(String jobHandle);
    }
}