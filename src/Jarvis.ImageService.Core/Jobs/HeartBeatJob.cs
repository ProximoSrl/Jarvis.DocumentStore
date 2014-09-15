using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Quartz;

namespace Jarvis.ImageService.Core.Jobs
{
    public class HeartBeatJob : IJob, IDisposable
    {
        public ILogger Logger { get; set; }
        public void Execute(IJobExecutionContext context)
        {
            Logger.DebugFormat("Alive @ {0} !", DateTime.Now);
        }


        public void Dispose()
        {
            Logger.DebugFormat("Disposed @ {0} !", DateTime.Now);
        }
    }
}
