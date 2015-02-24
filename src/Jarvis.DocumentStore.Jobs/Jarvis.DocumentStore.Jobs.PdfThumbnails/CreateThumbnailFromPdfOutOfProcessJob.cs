using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.JobsHost.Helpers;
using Jarvis.DocumentStore.Shared.Jobs;

namespace Jarvis.DocumentStore.Jobs.PdfThumbnails
{
    public class CreateThumbnailFromPdfOutOfProcessJob : AbstractOutOfProcessPollerJob
    {
        public CreateThumbnailFromPdfOutOfProcessJob(Func<CreateImageFromPdfTask> taskFactory)
        {
            base.PipelineId = "pdf";
            base.QueueName = "pdfThumb";
            _taskFactory = taskFactory;
        }

        private Func<CreateImageFromPdfTask> _taskFactory;

        protected async override System.Threading.Tasks.Task<bool> OnPolling(PollerJobParameters parameters, string workingFolder)
        {
            String format = parameters.All[JobKeys.ThumbnailFormat];

            Logger.DebugFormat("Conversion for jobId {0} in format {1} starting", parameters.JobId, format);

            var task = _taskFactory();
            var passwords = ClientPasswordSet.GetPasswordFor(parameters.FileName);
            foreach (var password in passwords)
            {
                task.Passwords.Add(password);
            }
            string pathToFile = await DownloadBlob(parameters.TenantId, parameters.JobId, parameters.FileName, workingFolder);

            var convertParams = new CreatePdfImageTaskParams()
            {
                Dpi = parameters.GetIntOrDefault(JobKeys.Dpi, 150),
                FromPage = parameters.GetIntOrDefault(JobKeys.PagesFrom, 1),
                Pages = parameters.GetIntOrDefault(JobKeys.PagesCount, 1),
                Format = (CreatePdfImageTaskParams.ImageFormat)Enum.Parse(typeof(CreatePdfImageTaskParams.ImageFormat), format, true)
            };

            await task.Run(
                pathToFile,
                convertParams,
                (i, s) => Write(workingFolder, parameters, format, i, s) //Currying
            );


            Logger.DebugFormat("Conversion of {0} in format {1} done", parameters.JobId, format);
            return true;
        }

        public async Task<Boolean> Write(String workerFolder, PollerJobParameters parameters, String format, int pageIndex, Stream stream)
        {
            var rawFileName = Path.Combine(workerFolder, Path.GetFileNameWithoutExtension(parameters.FileName) + ".page_" + pageIndex + "." + format);
            using (var outStream = File.OpenWrite(rawFileName))
            {
                stream.CopyTo(outStream);
            }

            await AddFormatToDocumentFromFile(
                 parameters.TenantId,
                 parameters.JobId,
                 new DocumentFormat(DocumentFormats.RasterImage),
                 rawFileName,
                new Dictionary<string, object>());
            return true;
        }
    }
}
