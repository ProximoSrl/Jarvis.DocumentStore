using System;
using Jarvis.DocumentStore.Core.Model;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public class JobTracker
    {
        public JobKey Id { get; private set; }
        public string JobType { get; set; }
        public FileId FileId { get; set; }
        public long Elapsed { get; set; }
        public string Message { get; set; }

        public JobTracker(JobKey jobKey, FileId fileId, string jobType)
        {
            this.Id = jobKey;
            this.FileId = fileId;
            this.JobType = jobType;
            this.Elapsed = - DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}