using System;
using System.Collections.Generic;
using System.IO;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.JobsHost.Helpers;
using Jarvis.DocumentStore.Shared.Jobs;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.HtmlZip
{
    public class HtmlToPdfOutOfProcessJob : AbstractOutOfProcessPollerJob
    {
        public HtmlToPdfOutOfProcessJob()
        {
            base.PipelineId = "htmlzip";
            base.QueueName = "htmlzip";
        }

        protected async override Task<ProcessResult> OnPolling(PollerJobParameters parameters, string workingFolder)
        {
            string pathToFile = await DownloadBlob(parameters.TenantId, parameters.JobId, parameters.FileName, workingFolder);
            String fileName = Path.Combine(Path.GetDirectoryName(pathToFile), parameters.All[JobKeys.FileName]);
            Logger.DebugFormat("Move blob id {0} to real filename {1}", pathToFile, fileName);
            if (File.Exists(fileName)) File.Delete(fileName);
            File.Copy(pathToFile, fileName);
            var converter = new HtmlToPdfConverterFromDiskFile(fileName, base.JobsHostConfiguration)
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
            return ProcessResult.Ok;
        }
    }
}
