using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.ImageService.Core.ProcessingPipeline.Conversions;
using Quartz;

namespace Jarvis.ImageService.Core.Jobs
{
    public class ConvertToPdfJob : IJob
    {
        private string FileId { get; set; }
        readonly ConvertToPdfTask _convertToPdfTask;

        public ILogger Logger { get; set; }

        public ConvertToPdfJob(ConvertToPdfTask convertToPdfTask)
        {
            _convertToPdfTask = convertToPdfTask;
        }

        public void Execute(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            FileId = jobDataMap.GetString(JobKeys.FileId);
            string extension = jobDataMap.GetString(JobKeys.FileExtension);

            if (_convertToPdfTask.CanHandle(extension))
            {
                Logger.DebugFormat(
                    "Delegating conversion of file {0} ({1}) to pdfTask",
                    FileId,
                    extension
                );
                _convertToPdfTask.Convert(FileId);
                return;
            }

            Logger.Error("Conversion handler not found");
        }
    }
}
