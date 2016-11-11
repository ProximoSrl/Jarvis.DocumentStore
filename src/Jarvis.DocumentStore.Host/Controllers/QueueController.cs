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

        /// <summary>
        /// For a given handle, it simply return the list of jobs that still are
        /// pending or executing. It is necessary to understand if a specific handle
        /// has finished all the conversion.
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("queue/getPending/{tenantId}/{handle}")]
        public String[] GetPending(TenantId tenantId, DocumentHandle handle)
        {
            using (Metric.Timer("queue-getPending", Unit.Requests).NewContext(handle))
            {
                if (QueueManager == null) return null;
                return QueueManager.GetPendingJobs(tenantId, handle);
            }
        }

        [HttpPost]
        [Route("queue/setjobcomplete")]
        public Boolean SetComplete(FinishedJobParameter parameter)
        {
            using (Metric.Timer("queue-complete", Unit.Requests).NewContext(parameter.QueueName))
            {
                if (parameter.ReQueue)
                {
                    var timespan = TimeSpan.FromSeconds(parameter.ReScheduleTimespanInSeconds);
                    return QueueManager.ReQueueJob(parameter.QueueName, parameter.JobId, parameter.ErrorMessage, timespan);
                }
                else
                {
                    return QueueManager.SetJobExecuted(parameter.QueueName, parameter.JobId, parameter.ErrorMessage);
                }
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

        /// <summary>
        /// True if queue manager should re-schedule the job in 
        /// the future
        /// </summary>
        public Boolean ReQueue { get; set; }

        /// <summary>
        /// If <paramref name="ReQueue"/> is true, this is the Timespan
        /// in seconds to reschedule the job.
        /// </summary>
        public Int32 ReScheduleTimespanInSeconds { get; set; }
    }
}
