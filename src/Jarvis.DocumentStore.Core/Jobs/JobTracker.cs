using System;
using Jarvis.DocumentStore.Core.Model;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public class JobTracker
    {
        public JobKey Id { get; private set; }

        public Int32 CompletedCount { get; set; }

        public Int32 FailureCount { get; set; }

        public Int32 RetryCount { get; set; }

        public JobTracker(JobKey jobKey)
        {
            this.Id = jobKey;
        }
    }

    public class TriggerTracker 
    {
        public TriggerKey Id { get; set; }

        public string JobType { get; set; }
        public BlobId BlobId { get; set; }
        public long Elapsed { get; set; }
        public string Message { get; set; }

        public TriggerTracker(TriggerKey triggerKey, BlobId blobId, string jobType)
        {
            this.Id = triggerKey;
            this.BlobId = blobId;
            this.JobType = jobType;
            this.Elapsed = - DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}