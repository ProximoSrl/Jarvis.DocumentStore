using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Quartz;

namespace Jarvis.DocumentStore.Host.Controllers
{
    /// <summary>
    /// Expose Scheduler commands.
    /// Scheduler can be null if not enabled for a worker
    /// </summary>
    public class SchedulerController : ApiController
    {
        public IScheduler Scheduler { get; set; }
        
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
    }
}
