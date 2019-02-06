using System.Collections.Generic;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.JobsHost.Helpers;
using Jarvis.DocumentStore.Shared.Jobs;
using System.Threading.Tasks;
using System.IO;
using Castle.Core.Logging;
using System;

namespace Jarvis.DocumentStore.Jobs.MsOffice
{
    /// <summary>
    /// Converts a file to pdf using headless libreoffice
    /// </summary>
    public class MicrosoftOfficePdfOutOfProcessJob : AbstractOutOfProcessPollerJob
    {
        private readonly WordConverter _wordConverter;
        private readonly PowerPointConverter _powerPointConverter;
        private readonly ExcelConverter _excelConverter;
        private readonly ILogger _logger;

        public MicrosoftOfficePdfOutOfProcessJob(
            WordConverter wordConverter,
            PowerPointConverter powerPointConverter,
            ExcelConverter excelConverter,
            ILogger logger)
        {
            base.PipelineId = "office";
            base.QueueName = "office";
            _wordConverter = wordConverter;
            _powerPointConverter = powerPointConverter;
            _excelConverter = excelConverter;
            _logger = logger;
            OfficeUtils.Logger = logger;
        }

        protected async override Task<ProcessResult> OnPolling(PollerJobParameters parameters, string workingFolder)
        {
            Logger.InfoFormat(
                "Delegating conversion of file {0} to Office automation for job {1} in working folder {2}",
                parameters.FileName,
                parameters.JobId,
                workingFolder
            );

            string pathToFile = await DownloadBlob(parameters.TenantId, parameters.JobId, parameters.FileName, workingFolder).ConfigureAwait(false);

            Logger.DebugFormat("Downloaded file {0} to be converted to pdf", pathToFile);
            var convertedFile = Path.ChangeExtension(pathToFile, ".pdf");

            ProcessResult conversionResult = ConvertFile(pathToFile, convertedFile);
            if (conversionResult != null)
            {
                if (conversionResult.Result)
                {
                    Logger.InfoFormat("File {0} correctly converted to PDF with office automation", parameters.FileName);
                    await AddFormatToDocumentFromFile(
                           parameters.TenantId,
                           parameters.JobId,
                           new Client.Model.DocumentFormat(DocumentFormats.Pdf),
                           convertedFile,
                           new Dictionary<string, object>()).ConfigureAwait(false);
                }
                return conversionResult;
            }
            return ProcessResult.Fail("Unknown error during office conversion");
        }

        private ProcessResult ConvertFile(string pathToFile, string convertedFile)
        {
            String conversionError = String.Empty;
            if (IsWordFile(pathToFile))
            {
                conversionError = _wordConverter.ConvertToPdf(pathToFile, convertedFile);
            }
            else if (IsPowerPointFile(pathToFile))
            {
                conversionError = _powerPointConverter.ConvertToPdf(pathToFile, convertedFile);
            }
            else if (IsExcelFile(pathToFile))
            {
                conversionError = _excelConverter.ConvertToPdf(pathToFile, convertedFile);
            }
            else
            {
                conversionError = $"Unable to convert file {pathToFile} unknown format for office file.";
            }

            if (!String.IsNullOrEmpty(conversionError))
            {
                _logger.Error(conversionError);
                return ProcessResult.Fail(conversionError);
            }

            return ProcessResult.Ok;
        }

        private bool IsExcelFile(string pathToFile)
        {
            return Path.GetExtension(pathToFile).Trim('.').StartsWith("xls", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsPowerPointFile(string pathToFile)
        {
            return Path.GetExtension(pathToFile).Trim('.').StartsWith("ppt", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsWordFile(string pathToFile)
        {
            return Path.GetExtension(pathToFile).Trim('.').StartsWith("doc", StringComparison.OrdinalIgnoreCase);
        }
    }
#pragma warning restore S2583 // Conditionally executed blocks should be reachable
#pragma warning restore S1854 // Dead stores should be removed
}
