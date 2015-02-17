using System;
using System.Collections.Generic;
using System.IO;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Jobs.OutOfProcessPollingJobs;
using Jarvis.DocumentStore.Shared.Jobs;

namespace Jarvis.DocumentStore.Jobs.HtmlZip
{
    public class HtmlToPdfOutOfProcessJob : AbstractOutOfProcessPollerFileJob
    {
        public HtmlToPdfOutOfProcessJob()
        {
            base.PipelineId = "htmlzip";
            base.QueueName = "htmlzip";
        }

        protected async override System.Threading.Tasks.Task<bool> OnPolling(PollerJobParameters parameters, string workingFolder)
        {
            string pathToFile = await DownloadBlob(parameters.TenantId, parameters.JobId, parameters.FileExtension, workingFolder);
            String fileName = Path.Combine(Path.GetDirectoryName(pathToFile), parameters.All[JobKeys.FileName]);
            Logger.DebugFormat("Move blob id {0} to real filename {1}", pathToFile, fileName);
            if (File.Exists(fileName)) File.Delete(fileName);
            File.Copy(pathToFile, fileName);
            var converter = new HtmlToPdfConverterFromDiskFile(fileName, ConfigService)
            {
                Logger = Logger
            };

            var pdfConvertedFileName = converter.Run(parameters.TenantId, parameters.JobId);
            await AddFormatToDocumentFromFile(
                parameters.TenantId,
                parameters.JobId,
                new  DocumentFormat(DocumentFormats.Pdf), 
                pdfConvertedFileName, 
                new Dictionary<string, object>());
            return true;
        }
    }
}
