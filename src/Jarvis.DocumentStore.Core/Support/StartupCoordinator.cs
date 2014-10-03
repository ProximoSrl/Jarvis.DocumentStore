using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;

namespace Jarvis.DocumentStore.Core.Support
{
    public class StartupCoordinator
    {
        public IScheduler Scheduler { get; set; }

        public void RebuildCompleted()
        {
            if(Scheduler != null)
                Scheduler.ResumeAll();
        }
    }
}
