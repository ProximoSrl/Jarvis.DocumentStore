using System;
using System.Collections.Generic;
using System.IO;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.JobsHost.Helpers;
using Jarvis.DocumentStore.Shared.Jobs;

namespace Jarvis.DocumentStore.Jobs.HtmlZipOld
{
    public class HtmlToPdfOutOfProcessJobOld : AbstractOutOfProcessPollerJob
    {
        public HtmlToPdfOutOfProcessJobOld()
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
            var converter = new HtmlToPdfConverterFromDiskFileOld(fileName, base.JobsHostConfiguration)
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
