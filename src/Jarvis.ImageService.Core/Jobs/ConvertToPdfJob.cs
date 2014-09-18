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
        readonly ConvertFileToPdfWithLibreOfficeTask _convertFileToPdfWithLibreOfficeTask;

        public ILogger Logger { get; set; }

        public ConvertToPdfJob(ConvertFileToPdfWithLibreOfficeTask convertFileToPdfWithLibreOfficeTask)
        {
            _convertFileToPdfWithLibreOfficeTask = convertFileToPdfWithLibreOfficeTask;
        }

        public void Execute(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            FileId = jobDataMap.GetString(JobKeys.FileId);
            string extension = jobDataMap.GetString(JobKeys.FileExtension);

            if (_convertFileToPdfWithLibreOfficeTask.CanHandle(extension))
            {
                Logger.DebugFormat(
                    "Delegating conversion of file {0} ({1}) to pdfTask",
                    FileId,
                    extension
                );
                _convertFileToPdfWithLibreOfficeTask.Convert(FileId);
                return;
            }

            Logger.Error("Conversion handler not found");
        }
    }
}
