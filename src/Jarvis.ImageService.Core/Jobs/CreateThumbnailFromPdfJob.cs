using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Quartz;

namespace Jarvis.ImageService.Core.Jobs
{
    public class CreateThumbnailFromPdfJob : IJob
    {
        public const string Documentid = "documentId";

        public ILogger Logger { get; set; }
        public void Execute(IJobExecutionContext context)
        {
            var jobData = context.JobDetail.JobDataMap[Documentid];
            Logger.DebugFormat("Pdf -> PNG of {0}", jobData);
            throw new Exception("Invalid!!!!!");
        }
    }
}
