using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Shared.Jobs
{
    public enum QueuedJobExecutionStatus
    {
        Idle = 0,
        Executing = 1,
        Failed = 2,
        Succeeded = 3,
        ReQueued = 4, //failed but it can be retried
    }

    public class QueuedJob
    {
        public String Id { get; set; }

        public Int64 StreamId { get; set; }

        public String TenantId { get; set; }

        public DateTime CreationTimestamp { get; set; }

        public Dictionary<String, String> Parameters { get; set; }

        public String ExecutionError { get; set; }

        public Int32 ErrorCount { get; set; }

        public DateTime ExecutionEndTime { get; set; }

        public DateTime? ExecutionStartTime { get; set; }

        public QueuedJobExecutionStatus Status { get; set; }

        public String ExecutingIdentity { get; set; }

        public String ExecutingHandle { get; set; }

        public Dictionary<String, Object> HandleCustomData { get; set; }

    }
}
