using System;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ProcessingPipeline.Conversions;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    /// <summary>
    /// Converts a file to pdf using headless libreoffice
    /// </summary>
    public class LibreOfficeToPdfJob : AbstractFileJob
    {
        readonly LibreOfficeConversion _libreOfficeConversion;

        public ILogger Logger { get; set; }

        public LibreOfficeToPdfJob(LibreOfficeConversion libreOfficeConversion)
        {
            _libreOfficeConversion = libreOfficeConversion;
        }

        public override void Execute(IJobExecutionContext context)
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
