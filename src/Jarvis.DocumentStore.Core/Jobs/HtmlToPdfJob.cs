using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ProcessingPipeline.Conversions;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public class HtmlToPdfJob : AbstractFileJob
    {
        public ConfigService ConfigService { get; set; }
        
        protected override void OnExecute(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            var fileId = new FileId(jobDataMap.GetString(JobKeys.FileId));

            var converter = new HtmlToPdfConverter(FileStore, ConfigService)
            {
                Logger = Logger
            };

            converter.Run(fileId);
        }
    }
}
