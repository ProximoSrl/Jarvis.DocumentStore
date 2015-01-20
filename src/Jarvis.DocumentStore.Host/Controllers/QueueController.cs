using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Quartz;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;

namespace Jarvis.DocumentStore.Host.Controllers
{
    /// <summary>
    /// Expose queues for callers.
    /// </summary>
    public class QueueController : ApiController
    {
        public IQueueDispatcher QueueDispatcher { get; set; }

        [HttpGet]
        [Route("queues/getnext/{queueName}")]
        public QueuedJob GetNextJob(String queueName)
        {
            return QueueDispatcher.GetNextJob(queueName);
        }
    }
}
