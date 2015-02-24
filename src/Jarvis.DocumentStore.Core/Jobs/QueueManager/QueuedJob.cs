using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Shared.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.Framework.Shared.MultitenantSupport;

namespace Jarvis.DocumentStore.Core.Jobs
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
        public QueuedJobId Id { get; set; }

        public BlobId BlobId { get; set; }

        public Int64 StreamId { get; set; }

        public TenantId TenantId { get; set; }

        public DocumentId DocumentId { get; set; }

        public DocumentHandle Handle { get; set; }

        /// <summary>
        /// This is the field used to determine job order execution
        /// </summary>
        public DateTime SchedulingTimestamp { get; set; }

        public Dictionary<String, String> Parameters { get; set; }

        public String ExecutionError { get; set; }

        public Int32 ErrorCount { get; set; }

        public DateTime? ExecutionEndTime { get; set; }

        public DateTime? ExecutionStartTime { get; set; }

        public QueuedJobExecutionStatus Status { get; set; }

        public String ExecutingIdentity { get; set; }

        public String ExecutingHandle { get; set; }

        public Dictionary<String, Object> HandleCustomData { get; set; }

        public static implicit operator QueuedJobDto(QueuedJob original) 
        {
            if (original == null) return null;
            return new QueuedJobDto()
            {
                Id = original.Id,
                Parameters = original.Parameters,
                HandleCustomData = original.HandleCustomData,
            };
        }



    }
}
