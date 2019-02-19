using System;
using System.Collections.Generic;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Shared.Jobs;
using Jarvis.Framework.Shared.MultitenantSupport;

namespace Jarvis.DocumentStore.Core.Jobs.QueueManager
{
    public enum QueuedJobExecutionStatus
    {
        Idle = 0, //it is important that idle is less than ReQueued because it is used as priority.
        Executing = 1,
        Failed = 2,
        Succeeded = 3,
        ReQueued = 4, //failed but it can be retried
    }

    /// <summary>
    /// This is the class that implement queued job for a single queue.
    /// </summary>
    public class QueuedJob
    {
        public QueuedJobId Id { get; set; }

        public BlobId BlobId { get; set; }

        public Int64 StreamId { get; set; }

        public TenantId TenantId { get; set; }

        public DocumentDescriptorId DocumentDescriptorId { get; set; }

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
