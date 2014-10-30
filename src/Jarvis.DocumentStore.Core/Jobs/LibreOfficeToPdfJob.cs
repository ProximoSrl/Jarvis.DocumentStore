using System;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    /// <summary>
    /// Converts a file to pdf using headless libreoffice
    /// </summary>
    public class LibreOfficeToPdfJob : AbstractFileJob
    {
        readonly ILibreOfficeConversion _libreOfficeConversion;

        public LibreOfficeToPdfJob(ILibreOfficeConversion libreOfficeConversion)
        {
            _libreOfficeConversion = libreOfficeConversion;
        }

        protected override void OnExecute(IJobExecutionContext context)
        {
            Logger.DebugFormat(
                "Delegating conversion of file {0} to libreoffice",
                this.FileId
            );

            var sourceFile = DownloadFileToWorkingFolder(this.FileId);
            var outputFile = _libreOfficeConversion.Run(sourceFile, "pdf");

            var newFileId = FileStore.Upload(outputFile);
            
            CommandBus.Send(new AddFormatToDocument(
                this.DocumentId, 
                new DocumentFormat(DocumentFormats.Pdf), 
                newFileId,
                this.PipelineId
            ));
        }
    }
}
