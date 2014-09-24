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
        public DateTime Started { get; set; }
        public DateTime? Ended { get; set; }
        public string Message { get; set; }

        public JobTracker(JobKey jobKey, FileId fileId, string jobType)
        {
            this.Id = jobKey;
            this.FileId = fileId;
            this.JobType = jobType;
            this.Started = DateTime.Now;
        }
    }
}