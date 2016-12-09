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

        public QueueHandler QueueHandler { get; set; }

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
                    new TenantId(parameter.TenantId),
                    parameter.QueueName,
                    parameter.Identity,
                    parameter.Handle,
                    parameter.CustomData);
            }
        }

        /// <summary>
        /// For a given handle, it simply return the list of jobs that still are
        /// pending or executing. It is necessary to understand if a specific handle
        /// has some pending job to be executed.
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("queue/getJobs/{tenantId}/{handle}")]
        public QueuedJobInfo[] GetJobs(TenantId tenantId, DocumentHandle handle)
        {
            using (Metric.Timer("queue-getPending", Unit.Requests).NewContext(handle))
            {
                if (QueueManager == null) return null;
                return QueueManager.GetJobsForHandle(tenantId, handle, null);
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
                    return QueueManager.ReQueueJob(parameter.QueueName, parameter.JobId, parameter.ErrorMessage, timespan, parameter.ParametersToModify);
                }
                else
                {
                    return QueueManager.SetJobExecuted(parameter.QueueName, parameter.JobId, parameter.ErrorMessage, parameter.ParametersToModify);
                }
            }
        }

        [HttpGet]
        [Route("queue/requeue/{tenantId}/{handle}")]
        public Boolean ReQueue(TenantId tenantId, DocumentHandle handle)
        {
            return QueueManager.ReQueueJobs(tenantId, handle);
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

        /// <summary>
        /// when a job is executed it can modify/insert some parameters, it could
        /// be useful both when a job is re-queued, or when a job fail, so it can
        /// store more information about failure.
        /// </summary>
        public Dictionary<String, String> ParametersToModify { get; set; }
    }
}
