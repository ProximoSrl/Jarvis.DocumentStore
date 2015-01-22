using System.IO;
using System.Linq;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing.Tools;
using Jarvis.DocumentStore.Core.Services;
using Quartz;
using Jarvis.DocumentStore.Core.Support;

namespace Jarvis.DocumentStore.Core.Jobs.PollingJobs
{
    public class ImageResizePollerJob : AbstractPollerFileJob
    {
        public ImageResizePollerJob()
        {
            base.PipelineId = new PipelineId("img");
            base.QueueName = "imgResize";
        }

        protected override void OnPolling(
            PollerJobBaseParameters baseParameters, 
            System.Collections.Generic.IDictionary<string, string> fullParameters, 
            Storage.IBlobStore currentTenantBlobStore, 
            string workingFolder)
        {
            var fileExtension = fullParameters[JobKeys.ThumbnailFormat];
            var sizesAsString = fullParameters[JobKeys.Sizes];
            var imageSizes = SizeInfoHelper.Deserialize(sizesAsString);

            Logger.DebugFormat("Starting resize job for {0} - {1}", baseParameters.InputBlobId, sizesAsString);

            var descriptor = currentTenantBlobStore.GetDescriptor(baseParameters.InputBlobId);

            using (var sourceStream = descriptor.OpenRead())
            {
                using (var pageStream = new MemoryStream())
                {
                    sourceStream.CopyTo(pageStream);

                    foreach (var size in imageSizes)
                    {
                        pageStream.Seek(0, SeekOrigin.Begin);
                        var fileFormat = new DocumentFormat("thumb." + size.Name);

                        var resizedImageFileName = new FileNameWithExtension(baseParameters.InputBlobId + "." + size.Name + "." + fileExtension);

                        using (var writer = currentTenantBlobStore.CreateNew(fileFormat, resizedImageFileName))
                        {
                            Logger.DebugFormat("Resizing {0} - {1}", baseParameters.InputBlobId, writer.BlobId);
                            ImageResizer.Shrink(pageStream, writer.WriteStream, size.Width, size.Height);

                            CommandBus.Send(new AddFormatToDocument(
                                baseParameters.InputDocumentId,
                                fileFormat,
                                writer.BlobId,
                                this.PipelineId
                            ));
                        }
                    }
                }
            }

            Logger.DebugFormat("Ended resize job for {0} - {1}", baseParameters.InputBlobId, sizesAsString);
        }
    }
}
