using Jarvis.DocumentStore.Core.Jobs.QueueManager;
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
        String Start(QueueInfo queueInfo, List<String> docStoreAddresses);

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

    /// <summary>
    /// Interface of a single job that uses poller queue for jobs scheduling.
    /// </summary>
    public interface IPollerJob 
    {
        String QueueName { get; }

        Boolean IsActive { get; }

        /// <summary>
        /// Tells me if the job can execute in process or is made to be executed
        /// in out of process. In the long run InProcess jobs will disappear over
        /// out of process jobs only, this will make this property obsolete and to remove.
        /// </summary>
        Boolean IsOutOfProcess { get;  }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="documentStoreAddressUrls">List of document store addressess</param>
        /// <param name="handle">The handle used to specify the identity of the worker returned from 
        /// a call to <see cref="IPollerJobManager.Start"/> </param>
        void Start(List<String> documentStoreAddressUrls, String handle);

        /// <summary>
        /// Stops the job.
        /// </summary>
        void Stop();
    }
}
