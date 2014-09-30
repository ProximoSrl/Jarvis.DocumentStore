using System;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ProcessingPipeline;
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
            Logger.DebugFormat(
                "Delegating conversion of file {0} to libreoffice",
                this.FileId
            );
            
            DateTime start = DateTime.Now;
            var newFileId = _libreOfficeConversion.Run(this.FileId, "pdf");

            CommandBus.Send(new AddFormatToDocument(
                this.DocumentId, 
                new DocumentFormat(DocumentFormats.Pdf), 
                newFileId
            ));
            
            var elapsed = DateTime.Now - start;
            Logger.DebugFormat("Libreoffice conversion task ended in {0}ms", elapsed.TotalMilliseconds);
        }
    }
}
