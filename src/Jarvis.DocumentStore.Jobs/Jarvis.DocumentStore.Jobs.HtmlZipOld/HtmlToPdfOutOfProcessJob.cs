using System;
using System.Collections.Generic;
using System.IO;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.JobsHost.Helpers;
using Jarvis.DocumentStore.Shared.Jobs;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.HtmlZipOld
{
    public class HtmlToPdfOutOfProcessJobOld : AbstractOutOfProcessPollerJob
    {
        public HtmlToPdfOutOfProcessJobOld()
        {
            base.PipelineId = "htmlzip";
            base.QueueName = "htmlzip";
        }

        protected async override Task<ProcessResult> OnPolling(PollerJobParameters parameters, string workingFolder)
        {
            string pathToFile = await DownloadBlob(parameters.TenantId, parameters.JobId, parameters.FileName, workingFolder);
            if (Logger.IsDebugEnabled)
                Logger.DebugFormat("Conversion of HtmlZip to PDF: file {0}", pathToFile);

            var file = pathToFile;
            if (pathToFile.ToLower().EndsWith(".mht") || pathToFile.ToLower().EndsWith(".mhtml"))
            {
                string mhtml = File.ReadAllText(pathToFile);
                MHTMLParser parser = new MHTMLParser(mhtml);
                parser.OutputDirectory = workingFolder;
                parser.DecodeImageData = true;
                var outFile = Path.ChangeExtension(pathToFile, ".html");
                File.WriteAllText(outFile, parser.getHTMLText());
                file = outFile;
            }

            var converter = new HtmlToPdfConverterFromDiskFileOld(file, base.JobsHostConfiguration)
            {
                Logger = Logger
            };

            var pdfConvertedFileName = converter.Run(parameters.JobId);
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
