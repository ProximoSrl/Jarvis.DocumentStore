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

        public LibreOfficeToPdfJob(LibreOfficeConversion libreOfficeConversion)
        {
            _libreOfficeConversion = libreOfficeConversion;
        }

        protected override void OnExecute(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            var fileId = new FileId(jobDataMap.GetString(JobKeys.FileId));

            Logger.DebugFormat(
                "Delegating conversion of file {0} to libreoffice",
                fileId
            );
            
            DateTime start = DateTime.Now;
            _libreOfficeConversion.Run(fileId, "pdf");
            var elapsed = DateTime.Now - start;
            Logger.DebugFormat("Libreoffice conversion task ended in {0}ms", elapsed.TotalMilliseconds);
        }
    }
}
