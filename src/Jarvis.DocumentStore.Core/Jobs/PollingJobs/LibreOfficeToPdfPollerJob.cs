using System;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs.PollingJobs
{
    /// <summary>
    /// Converts a file to pdf using headless libreoffice
    /// </summary>

    public class LibreOfficeToPdfPollerJob : AbstractPollerFileJob
    {

        public LibreOfficeToPdfPollerJob()
        {
            base.PipelineId = new PipelineId("office");
            base.QueueName = "office";
        }


        protected override void OnPolling(
            PollerJobBaseParameters baseParameters, 
            System.Collections.Generic.IDictionary<string, string> fullParameters, 
            Storage.IBlobStore currentTenantBlobStore, 
            string workingFolder)
        {
            Logger.DebugFormat(
               "Delegating conversion of file {0} to libreoffice",
               baseParameters.InputBlobId
           );

            //libreofficeconversion is registered per tenant.
            var _libreOfficeConversion = TenantAccessor.Current.Container.Resolve<ILibreOfficeConversion>();
            var sourceFile = currentTenantBlobStore.Download(baseParameters.InputBlobId, workingFolder);
            var outputFile = _libreOfficeConversion.Run(sourceFile, "pdf");

            var newBlobId = currentTenantBlobStore.Upload(DocumentFormats.Pdf, outputFile);

            CommandBus.Send(new AddFormatToDocument(
                baseParameters.InputDocumentId,
                DocumentFormats.Pdf,
                newBlobId,
                this.PipelineId
            ));
        }
    }
}
