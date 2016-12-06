using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Shared.Jobs
{
    /// <summary>
    /// Used to retrieve data for jobs of a specific handle.
    /// </summary>
    public class QueuedJobInfo
    {
        public QueuedJobInfo(string jobId, string queueName, bool executed, bool success)
        {
            JobId = jobId;
            QueueName = queueName;
            Executed = executed;
            Success = success;
        }

        public String JobId { get; set; }

        /// <summary>
        /// Name of the queue.
        /// </summary>
        public String QueueName { get; set; }

        /// <summary>
        /// True if the job was executed, 
        /// </summary>
        public Boolean Executed { get; set; }

        /// <summary>
        /// This property has meaning only if <see cref="Executed"/> is true, and contains
        /// true if the execution was successfully.
        /// </summary>
        public Boolean Success { get; set; }

    }
}
