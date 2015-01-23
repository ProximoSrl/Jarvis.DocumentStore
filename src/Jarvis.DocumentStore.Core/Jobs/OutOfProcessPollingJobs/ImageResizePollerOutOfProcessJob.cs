using System.Collections.Generic;
using System.IO;
using Castle.MicroKernel.Registration;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Jobs.PollingJobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing.Tools;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Core.Support;

namespace Jarvis.DocumentStore.Core.Jobs.OutOfProcessPollingJobs
{
    public class ImageResizePollerOutOfProcessJob : AbstractOutOfProcessPollerFileJob
    {
        public ImageResizePollerOutOfProcessJob()
        {
            base.PipelineId = new PipelineId("img");
            base.QueueName = "imgResize";
        }

        protected async override System.Threading.Tasks.Task<bool> OnPolling(PollerJobParameters parameters, string workingFolder)
        {
            var fileExtension = parameters.All[JobKeys.ThumbnailFormat];
            var sizesAsString = parameters.All[JobKeys.Sizes];
            var imageSizes = SizeInfoHelper.Deserialize(sizesAsString);

            Logger.DebugFormat("Starting resize job for {0} - {1}", parameters.InputBlobId, sizesAsString);

            string pathToFile = await DownloadBlob(parameters.TenantId, parameters.InputBlobId, parameters.FileExtension, workingFolder);
            

            using (var sourceStream = File.OpenRead(pathToFile))
            {
                using (var pageStream = new MemoryStream())
                {
                    sourceStream.CopyTo(pageStream);

                    foreach (var size in imageSizes)
                    {
                        Logger.DebugFormat("Resize job for {0} - {1}", parameters.InputBlobId, size.Name);
                        pageStream.Seek(0, SeekOrigin.Begin);
                        var fileFormat = new Client.Model.DocumentFormat("thumb." + size.Name);

                        string resizeImagePath = Path.Combine(workingFolder, size.Name + "." + fileExtension);
                        using (var outStream = File.OpenWrite(resizeImagePath))
                        {
                            Logger.DebugFormat("Resizing {0}}", parameters.InputBlobId);
                            ImageResizer.Shrink(pageStream, outStream, size.Width, size.Height);
                        }
                       await AddFormatToDocumentFromFile(parameters.TenantId, parameters.InputDocumentId, fileFormat,
                            resizeImagePath, new Dictionary<string, object>());
                       
                    }
                }
            }

            Logger.DebugFormat("Ended resize job for {0} - {1}", parameters.InputBlobId, sizesAsString);
            return true;
        }
    }
}
