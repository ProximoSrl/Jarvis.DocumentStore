using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Quartz;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Jarvis.DocumentStore.Shared.Jobs;

namespace Jarvis.DocumentStore.Host.Controllers
{
    /// <summary>
    /// Expose queues for callers.
    /// </summary>
    public class QueueController : ApiController
    {
        public IQueueDispatcher QueueDispatcher { get; set; }

        [HttpPost]
        [Route("queue/getnextjob")]
        public QueuedJob GetNextJob(GetNextJobParameter parameter)
        {
            if (QueueDispatcher == null) return null;
            return QueueDispatcher.GetNextJob(parameter.QueueName, parameter.Identity, parameter.Handle);
        }

        [HttpPost]
        [Route("queue/setjobcomplete")]
        public Boolean SetComplete(FinishedJobParameter parameter)
        {
            return QueueDispatcher.SetJobExecuted(parameter.QueueName, parameter.JobId, parameter.ErrorMessage);
        }
    }

    public class GetNextJobParameter 
    {
        public String QueueName { get; set; }

        public String Identity { get; set; }

        public String Handle { get; set; }
    }

    public class FinishedJobParameter 
    {
        public String QueueName { get; set; }

        public String ErrorMessage { get; set; }

        public String JobId { get; set; }
    }
}
