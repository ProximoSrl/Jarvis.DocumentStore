using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.JobsHost.Helpers;
using Jarvis.DocumentStore.Shared.Jobs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.MsGraphOfficePdfThumbnails
{
    /// <summary>
    /// Converts a file to pdf using headless libreoffice
    /// </summary>
    public class MsGraphOfficePdfThumbnailOutOfProcessJob : AbstractOutOfProcessPollerJob
    {
        private readonly MsGraphOfficePdfThumbnail _msGraphOfficeToPdfConverter;

        public MsGraphOfficePdfThumbnailOutOfProcessJob(
            MsGraphOfficePdfThumbnail msGraphOfficeToPdfConverter)
        {
            PipelineId = "pdf";
            QueueName = "pdfThumb";
            _msGraphOfficeToPdfConverter = msGraphOfficeToPdfConverter;
        }

        protected async override Task<ProcessResult> OnPolling(PollerJobParameters parameters, string workingFolder)
        {
            Logger.InfoFormat(
                "Delegating conversion of file {0} to Office365 for job {1} in working folder {2}",
                parameters.FileName,
                parameters.JobId,
                workingFolder
            );

            string pathToFile = await DownloadBlob(parameters.TenantId, parameters.JobId, parameters.FileName, workingFolder).ConfigureAwait(false);

            var conversionResult = await _msGraphOfficeToPdfConverter.CreateThumbnail(pathToFile, workingFolder);
            if (!string.IsNullOrEmpty(conversionResult))
            {
                Logger.InfoFormat("File {0} correctly converted to PDF with office365 graph api", parameters.FileName);
                await AddFormatToDocumentFromFile(
                       parameters.TenantId,
                       parameters.JobId,
                       new DocumentFormat(DocumentFormats.RasterImage),
                       conversionResult,
                       new Dictionary<string, object>()).ConfigureAwait(false);

                return ProcessResult.Ok;
            }
            return ProcessResult.Fail("Unknown error during office conversion");
        }
    }
#pragma warning restore S2583 // Conditionally executed blocks should be reachable
#pragma warning restore S1854 // Dead stores should be removed
}
