using System.IO;
using System.Linq;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing.Tools;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Quartz;
using Jarvis.DocumentStore.Core.Support;

namespace Jarvis.DocumentStore.Core.Jobs.PollingJobs
{
    public class ImageResizePollerJob : AbstractInProcessPollerFileJob
    {
        public ImageResizePollerJob()
        {
            base.PipelineId = new PipelineId("img");
            base.QueueName = "imgResize";
        }

        protected override void OnPolling(PollerJobParameters parameters, IBlobStore currentTenantBlobStore, string workingFolder)
        {
            var fileExtension = parameters.All[JobKeys.ThumbnailFormat];
            var sizesAsString = parameters.All[JobKeys.Sizes];
            var imageSizes = SizeInfoHelper.Deserialize(sizesAsString);

            Logger.DebugFormat("Starting resize job for {0} - {1}", parameters.InputBlobId, sizesAsString);

            var descriptor = currentTenantBlobStore.GetDescriptor(parameters.InputBlobId);

            using (var sourceStream = descriptor.OpenRead())
            {
                using (var pageStream = new MemoryStream())
                {
                    sourceStream.CopyTo(pageStream);

                    foreach (var size in imageSizes)
                    {
                        pageStream.Seek(0, SeekOrigin.Begin);
                        var fileFormat = new DocumentFormat("thumb." + size.Name);

                        var resizedImageFileName = new FileNameWithExtension(parameters.InputBlobId + "." + size.Name + "." + fileExtension);

                        using (var writer = currentTenantBlobStore.CreateNew(fileFormat, resizedImageFileName))
                        {
                            Logger.DebugFormat("Resizing {0} - {1}", parameters.InputBlobId, writer.BlobId);
                            ImageResizer.Shrink(pageStream, writer.WriteStream, size.Width, size.Height);

                            CommandBus.Send(new AddFormatToDocument(
                                parameters.InputDocumentId,
                                fileFormat,
                                writer.BlobId,
                                this.PipelineId
                            ));
                        }
                    }
                }
            }

            Logger.DebugFormat("Ended resize job for {0} - {1}", parameters.InputBlobId, sizesAsString);
        }
    }
}
