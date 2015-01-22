using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Shared.Jobs
{
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

        public Boolean Executing { get; set; }

        public String ExecutingIdentity { get; set; }

        public Boolean Finished { get; set; }


    }
}
