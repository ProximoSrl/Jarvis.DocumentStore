using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Jarvis.Framework.Shared.MultitenantSupport;
using Quartz;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Jarvis.DocumentStore.Shared.Jobs;
using Metrics;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Host.Controllers
{
    /// <summary>
    /// Expose queues for callers.
    /// </summary>
    public class QueueController : ApiController
    {
        public IQueueManager QueueManager { get; set; }

        public QueueController(IQueueManager queueManager)
        {
            QueueManager = queueManager;
        }

        public QueueController()
        {

        }

        [HttpPost]
        [Route("queue/getnextjob")]
        public QueuedJobDto GetNextJob(GetNextJobParameter parameter)
        {
            using (Metric.Timer("queue-nextjob", Unit.Requests).NewContext(parameter.QueueName))
            {
                if (QueueManager == null) return null;
                return QueueManager.GetNextJob(
                    parameter.QueueName,
                    parameter.Identity,
                    parameter.Handle,
                    new TenantId(parameter.TenantId),
                    parameter.CustomData);
            }
        }

        [HttpPost]
        [Route("queue/setjobcomplete")]
        public Boolean SetComplete(FinishedJobParameter parameter)
        {
            using (Metric.Timer("queue-complete", Unit.Requests).NewContext(parameter.QueueName))
            {
                return QueueManager.SetJobExecuted(parameter.QueueName, parameter.JobId, parameter.ErrorMessage);
            }
        }

        [HttpGet]
        [Route("queue/requeue/{tenantId}/{handle}")]
        public Boolean ReQueue(TenantId tenantId, DocumentHandle handle)
        {
            return QueueManager.ReQueueJobs(handle, tenantId);
        }
    }

    public class GetNextJobParameter 
    {
        public String QueueName { get; set; }

        public String Identity { get; set; }

        public String Handle { get; set; }

        public String TenantId { get; set; }

        public Dictionary<String, Object> CustomData { get; set; }
    }

    public class FinishedJobParameter 
    {
        public String QueueName { get; set; }

        public String ErrorMessage { get; set; }

        public String JobId { get; set; }
    }
}
