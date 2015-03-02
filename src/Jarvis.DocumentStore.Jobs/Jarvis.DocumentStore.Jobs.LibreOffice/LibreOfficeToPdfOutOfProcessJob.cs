using System.Collections.Generic;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.JobsHost.Helpers;
using Jarvis.DocumentStore.Shared.Jobs;

namespace Jarvis.DocumentStore.Jobs.LibreOffice
{
    /// <summary>
    /// Converts a file to pdf using headless libreoffice
    /// </summary>

    public class LibreOfficeToPdfOutOfProcessJob : AbstractOutOfProcessPollerJob
    {
        private ILibreOfficeConversion _conversion;

        public LibreOfficeToPdfOutOfProcessJob(ILibreOfficeConversion conversion)
        {
            base.PipelineId = "office";
            base.QueueName = "office";
            _conversion = conversion;
            _conversion.Initialize();
        }

        protected async override System.Threading.Tasks.Task<bool> OnPolling(PollerJobParameters parameters, string workingFolder)
        {
            Logger.DebugFormat(
               "Delegating conversion of file {0} to libreoffice",
               parameters.JobId
           );

            //libreofficeconversion is registered per tenant.

            string pathToFile = await DownloadBlob(parameters.TenantId, parameters.JobId, parameters.FileName, workingFolder);
            
            Logger.DebugFormat("Downloaded file {0} to be converted to pdf", pathToFile);
            var outputFile = _conversion.Run(pathToFile, "pdf");

            await AddFormatToDocumentFromFile(
                parameters.TenantId,
                parameters.JobId,
                new DocumentFormat(DocumentFormats.Pdf),
                outputFile,
                new Dictionary<string, object>());
            return true;
        }
    }
}
