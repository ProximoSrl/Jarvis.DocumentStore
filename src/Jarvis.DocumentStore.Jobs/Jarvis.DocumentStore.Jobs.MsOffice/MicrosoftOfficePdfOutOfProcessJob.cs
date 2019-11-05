using System.Collections.Generic;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.JobsHost.Helpers;
using Jarvis.DocumentStore.Shared.Jobs;
using System.Threading.Tasks;
using System.IO;
using Castle.Core.Logging;
using System;
using System.Configuration;
using System.Timers;

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
        private readonly Int32 _threadNumber;
        private readonly Timer _cleanupTimer;

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

            //var config = ConfigurationManager.AppSettings["threadNumber"];
            //_logger.InfoFormat("Configuration ThreadNumber is {0}", config);
            //if (String.IsNullOrEmpty(config) || !Int32.TryParse(config, out _threadNumber))
            //{
            //    _logger.Info("Configuration ThreadNumber wrong the job will default to a single thread");
            //    _threadNumber = 1;
            //}

            //It is not safe to have more than one thread running office automation.
            _threadNumber = 1; 

            OfficeUtils.KillStaleOfficeProgram();
            _cleanupTimer = new Timer();
            _cleanupTimer.Elapsed += (s, e) => OfficeUtils.KillStaleOfficeProgram();
            _cleanupTimer.Interval = 1000 * 60 * 10;
            _cleanupTimer.Start();
        }

        protected override int ThreadNumber => _threadNumber;

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
            string extension = Path.GetExtension(pathToFile).Trim('.');
            if (String.IsNullOrEmpty(extension))
                return false;

            return extension.StartsWith("xls", StringComparison.OrdinalIgnoreCase)
                || extension.Equals("xlsx", StringComparison.OrdinalIgnoreCase)
                || extension.Equals("xlsm", StringComparison.OrdinalIgnoreCase)
                || extension.Equals("xlsm", StringComparison.OrdinalIgnoreCase)
                || extension.Equals("ods", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsPowerPointFile(string pathToFile)
        {
            string extension = Path.GetExtension(pathToFile).Trim('.');
            if (String.IsNullOrEmpty(extension))
                return false;

            return extension.StartsWith("ppt", StringComparison.OrdinalIgnoreCase)
                || extension.Equals("ppsx", StringComparison.OrdinalIgnoreCase)
                || extension.Equals("pps", StringComparison.OrdinalIgnoreCase)
                || extension.Equals("ppsx", StringComparison.OrdinalIgnoreCase)
                || extension.Equals("odp", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsWordFile(string pathToFile)
        {
            string extension = Path.GetExtension(pathToFile).Trim('.');
            if (String.IsNullOrEmpty(extension))
                return false;

            return extension.StartsWith("doc", StringComparison.OrdinalIgnoreCase)
                || extension.StartsWith("docx", StringComparison.OrdinalIgnoreCase)
                || extension.Equals("rtf", StringComparison.OrdinalIgnoreCase)
                || extension.Equals("odt", StringComparison.OrdinalIgnoreCase);
        }
    }
#pragma warning restore S2583 // Conditionally executed blocks should be reachable
#pragma warning restore S1854 // Dead stores should be removed
}
