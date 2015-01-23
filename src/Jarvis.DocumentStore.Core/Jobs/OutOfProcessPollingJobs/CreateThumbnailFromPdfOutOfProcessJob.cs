using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Jobs.PollingJobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing.Pdf;
using Jarvis.DocumentStore.Core.Storage;
using DocumentFormats = Jarvis.DocumentStore.Core.Processing.DocumentFormats;

namespace Jarvis.DocumentStore.Core.Jobs.OutOfProcessPollingJobs
{
    public class CreateThumbnailFromPdfOutOfProcessJob : AbstractOutOfProcessPollerFileJob
    {
        public CreateThumbnailFromPdfOutOfProcessJob()
        {
            base.PipelineId = new PipelineId("pdf");
            base.QueueName = "pdfThumb";
        }

        protected async override System.Threading.Tasks.Task<bool> OnPolling(PollerJobParameters parameters, string workingFolder)
        {
            String format = parameters.All[JobKeys.ThumbnailFormat];

            Logger.DebugFormat("Conversion of {0} ({1}) in format {2} starting", parameters.InputDocumentId, parameters.InputBlobId, format);

            var task = new CreateImageFromPdfTask { Logger = Logger };
            string pathToFile = await DownloadBlob(parameters.TenantId, parameters.InputBlobId, parameters.FileExtension, workingFolder);

            using (var sourceStream = File.OpenRead(pathToFile))
            {
                var convertParams = new CreatePdfImageTaskParams()
                {

                    Dpi = parameters.All.GetIntOrDefault(JobKeys.Dpi, 150),
                    FromPage = parameters.All.GetIntOrDefault(JobKeys.PagesFrom, 1),
                    Pages = parameters.All.GetIntOrDefault(JobKeys.PagesCount, 1),
                    Format = (CreatePdfImageTaskParams.ImageFormat)Enum.Parse(typeof(CreatePdfImageTaskParams.ImageFormat), format, true)
                };

                task.Run(
                    sourceStream,
                    convertParams,
                    (i, s) => Write(parameters, format, i, s) //Currying
                );
            }

            Logger.DebugFormat("Conversion of {0} in format {1} done", parameters.InputBlobId, format);
            return true;
        }

        public async Task<Boolean> Write(PollerJobParameters paramters, String format, int pageIndex, Stream stream)
        {
            var fileName = new FileNameWithExtension(paramters.InputBlobId + ".page_" + pageIndex + "." + format);
            using (var outStream = File.OpenWrite(fileName))
            {
                stream.CopyTo(outStream);
            }

            await AddFormatToDocumentFromFile(
                 paramters.TenantId,
                 paramters.InputDocumentId,
                 new DocumentFormat(DocumentFormats.RasterImage),
                 fileName,
                new Dictionary<string, object>());
            return true;
        }
    }
}
