using System;
using System.Collections.Generic;

namespace Jarvis.DocumentStore.Shared.Jobs
{
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
        /// a call to IPollerJobManager.Start</param>
        void Start(List<String> documentStoreAddressUrls, String handle);

        /// <summary>
        /// Stops the job.
        /// </summary>
        void Stop();
    }
}
