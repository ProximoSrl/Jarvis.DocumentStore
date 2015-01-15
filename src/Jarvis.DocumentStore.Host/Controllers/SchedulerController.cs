using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Quartz;
using Jarvis.DocumentStore.Core.Jobs;

namespace Jarvis.DocumentStore.Host.Controllers
{
    /// <summary>
    /// Expose Scheduler commands.
    /// Scheduler can be null if not enabled for a worker
    /// </summary>
    public class SchedulerController : ApiController
    {
        public IScheduler Scheduler { get; set; }
        public JobStats JobStats { get; set; }

        
        [HttpPost]
        [Route("scheduler/start")]
        public void Start()
        {
            if (Scheduler != null) { 
                Scheduler.Start();
                Scheduler.ResumeAll();
            }
        }

        [HttpPost]
        [Route("scheduler/stop")]
        public void Stop()
        {
            if (Scheduler != null )
                Scheduler.Standby();
        }

        [HttpGet]
        [Route("scheduler/running")]
        public bool IsRunning()
        {
            if (Scheduler != null)
                return !Scheduler.InStandbyMode;

            return false;
        }

        [HttpGet]
        [Route("scheduler/stats")]
        public object Stats()
        {
            var triggerStats = this.JobStats.GetTriggerStats();

            return triggerStats;
        }
    }
}
