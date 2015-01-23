using System.Collections.Generic;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Jobs.PollingJobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using Jarvis.DocumentStore.Core.Storage;
using DocumentFormats = Jarvis.DocumentStore.Core.Processing.DocumentFormats;

namespace Jarvis.DocumentStore.Core.Jobs.OutOfProcessPollingJobs
{
    /// <summary>
    /// Converts a file to pdf using headless libreoffice
    /// </summary>

    public class LibreOfficeToPdfOutOfProcessJob : AbstractOutOfProcessPollerFileJob
    {
        private ILibreOfficeConversion _conversion;
        public LibreOfficeToPdfOutOfProcessJob(ILibreOfficeConversion conversion)
        {
            base.PipelineId = new PipelineId("office");
            base.QueueName = "office";
            _conversion = conversion;
        }

        protected async override System.Threading.Tasks.Task<bool> OnPolling(PollerJobParameters parameters, string workingFolder)
        {
            Logger.DebugFormat(
               "Delegating conversion of file {0} to libreoffice",
               parameters.InputBlobId
           );

            //libreofficeconversion is registered per tenant.

            string pathToFile = await DownloadBlob(parameters.TenantId, parameters.InputBlobId, workingFolder);

            var outputFile = _conversion.Run(pathToFile, "pdf");

            await AddFormatToDocumentFromFile(
                parameters.TenantId,
                parameters.InputDocumentId,
                new DocumentFormat(DocumentFormats.Pdf),
                outputFile,
                new Dictionary<string, object>());
            return true;
        }
    }
}
