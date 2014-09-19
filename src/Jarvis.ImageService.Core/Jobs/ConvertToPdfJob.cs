using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.ProcessingPipeline.Conversions;
using Quartz;

namespace Jarvis.ImageService.Core.Jobs
{
    /// <summary>
    /// Converts a file to pdf using headless liberoffice
    /// </summary>
    public class ConvertToPdfJob : IJob
    {
        readonly LibreOfficeConversion _libreOfficeConversion;

        public ILogger Logger { get; set; }

        public ConvertToPdfJob(LibreOfficeConversion libreOfficeConversion)
        {
            _libreOfficeConversion = libreOfficeConversion;
        }

        public void Execute(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            var fileId = new FileId(jobDataMap.GetString(JobKeys.FileId));
            string extension = jobDataMap.GetString(JobKeys.FileExtension);

            if (_libreOfficeConversion.CanHandle(extension))
            {
                Logger.DebugFormat(
                    "Delegating conversion of file {0} ({1}) to libreoffice",
                    fileId,
                    extension
                );
                DateTime start = DateTime.Now;
                _libreOfficeConversion.Run(fileId, "pdf");
                var elapsed = DateTime.Now - start;
                Logger.DebugFormat("Libreoffice conversion task ended in {0}ms", elapsed.TotalMilliseconds);
                return;
            }

            Logger.Error("Conversion handler not found");
        }
    }
}
