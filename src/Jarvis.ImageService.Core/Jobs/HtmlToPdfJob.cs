using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.ProcessingPipeline.Conversions;
using Jarvis.ImageService.Core.Services;
using Jarvis.ImageService.Core.Storage;
using Quartz;

namespace Jarvis.ImageService.Core.Jobs
{
    public class HtmlToPdfJob : AbstractFileJob
    {
        public IFileStore FileStore { get; set; }
        public ConfigService ConviService { get; set; }
        public ILogger Logger { get; set; }
        
        public override void Execute(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            var fileId = new FileId(jobDataMap.GetString(JobKeys.FileId));

            var converter = new HtmlToPdfConverter(FileStore, ConviService)
            {
                Logger = Logger
            };

            converter.Run(fileId);
        }
    }
}
