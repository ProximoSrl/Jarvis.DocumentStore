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
