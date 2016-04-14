using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Quartz;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;

namespace Jarvis.DocumentStore.Host.Controllers
{
    /// <summary>
    /// Expose Scheduler commands.
    /// Scheduler can be null if not enabled for a worker
    /// </summary>
    public class SchedulerController : ApiController
    {
        public IScheduler Scheduler { get; set; }

        public QueuedJobStatus QueuedJobStats { get; set; }

        public IQueueManager QueueDispatcher { get; set; }

        public DocumentStoreConfiguration Config { get; set; }

        public IPollerJobManager PollerJobManager { get; set; }

        public PollerManager PollerManager { get; set; }

        public SchedulerController(QueuedJobStatus queuedJobStats)
        {
            QueuedJobStats = queuedJobStats;
        }

        [HttpPost]
        [Route("scheduler/start")]
        public void Start()
        {
            throw new NotImplementedException("Implement how to start the queue.");
        }

        [HttpPost]
        [Route("scheduler/stop")]
        public void Stop()
        {
            throw new NotImplementedException("Implement how to stop the queue.");
        }

        [HttpGet]
        [Route("scheduler/running")]
        public bool IsRunning()
        {
            return true;
        }

        [HttpPost]
        [Route("scheduler/reschedulefailed/{queueName}")]
        public object RescheduleFailed(String queueName)
        {
            if (QueueDispatcher != null)
            {
                var success = QueueDispatcher.ReScheduleFailed(queueName);
                return new { Success = success };
            }
            return new { Error = "Queue manager infrastructure not started" };
        }


        [HttpGet]
        [Route("scheduler/getjobsinfo")]
        public object GetAllJobsInfo()
        {
            if (PollerJobManager != null)
            {
                return PollerJobManager.GetAllJobsInfo();
            }
            return new List<PollingJobInfo>();
        }

        [HttpPost]
        [Route("scheduler/restartworker/{queueName}")]
        public object RestartWorker(String queueName)
        {
            if (PollerManager != null)
            {
                PollerManager.RestartWorker(queueName, true);
                return new { Success = true };
            }
            return new { Error = "Queue manager infrastructure not started" };
        }

        [HttpPost]
        [Route("scheduler/suspendworker/{queueName}")]
        public object SuspendWorker(String queueName)
        {
            if (PollerManager != null)
            {
                var stopQueueResult = PollerManager.SuspendWorker(queueName);
                return new { Success = stopQueueResult };
            }
            return new { Error = "Queue manager infrastructure not started" };
        }

        [HttpPost]
        [Route("scheduler/resumeworker/{queueName}")]
        public object ResumeWorker(String queueName)
        {
            if (PollerManager != null)
            {
                PollerManager.RestartWorker(queueName, false);
                return new { Success = true };
            }
            return new { Error = "Queue manager infrastructure not started" };
        }

        [HttpGet]
        [Route("scheduler/stats")]
        public object Stats()
        {
            //avoid exception while the queue engine is starting.
            if (this.QueuedJobStats == null)
                return null;
            var queueStats = this.QueuedJobStats.GetQueuesStatus();
            return queueStats;
        }
    }
}
