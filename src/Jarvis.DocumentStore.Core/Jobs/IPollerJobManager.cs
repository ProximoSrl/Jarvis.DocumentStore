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
        /// Stops poller job, it disable the queue for this instalnce of Document store, closes the 
        /// queue and stop using it.
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
        /// <param name="forceClose">if true the job is killed if it is still active, it is useful
        /// if the job is stuck for some reason</param>
        /// <returns></returns>
        Boolean RestartWorker(String jobHandle, Boolean forceClose);

        /// <summary>
        /// We can suspend a queue, basically stops serving the queue, this is useful to limit load when
        /// server is highly loaded, we can suspend memory or CPU intensive queue.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        Boolean SuspendWorker(string handle);

        /// <summary>
        /// Retrieve info about all jobs, with standard <see cref="OutOfProcessBaseJobManager"/> this is the 
        /// list of the processes that should be active.
        /// </summary>
        /// <remarks>This list returns all the jobs, even the jobs that are stopped because failed the start test. This is used
        /// to understand all information about jobs</remarks>
        /// <returns></returns>
        List<PollingJobInfo> GetAllJobsInfo();

    }

    public class PollingJobInfo
    {
        public String QueueId { get; set; }

        /// <summary>
        /// true if the Worker is active and running.
        /// </summary>
        public Boolean WorkerActive { get; set; }

        /// <summary>
        /// Tells if the queue is active
        /// </summary>
        public Boolean QueueActive { get; set; }

        /// <summary>
        /// Description, for local job processes is the command line.
        /// </summary>
        public String ProcessDescription { get; set; }
    }
}