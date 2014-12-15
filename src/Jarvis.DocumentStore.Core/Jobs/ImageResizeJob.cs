using System.IO;
using System.Linq;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing.Tools;
using Jarvis.DocumentStore.Core.Services;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public class ImageResizeJob : AbstractFileJob
    {
        protected override void OnExecute(IJobExecutionContext context)
        {
            var jobDataMap = context.MergedJobDataMap;
            var fileExtension = jobDataMap.GetString(JobKeys.FileExtension);
            var sizesAsString = jobDataMap.GetString(JobKeys.Sizes);
            var sizes = sizesAsString.Split('|');

            Logger.DebugFormat("Starting resize job for {0} - {1}", this.InputBlobId, sizesAsString);

            var imageSizes = ConfigService.GetDefaultSizes().Where(x => sizes.Contains(x.Name)).ToArray();
            var descriptor = BlobStore.GetDescriptor(this.InputBlobId);

            using (var sourceStream = descriptor.OpenRead())
            {
                using (var pageStream = new MemoryStream())
                {
                    sourceStream.CopyTo(pageStream);

                    foreach (var size in imageSizes)
                    {
                        pageStream.Seek(0, SeekOrigin.Begin);
                        var fileFormat = new DocumentFormat("thumb." + size.Name);

                        var resizedImageFileName = new FileNameWithExtension(this.InputBlobId + "." + size.Name + "." + fileExtension);

                        using (var writer = BlobStore.CreateNew(fileFormat,resizedImageFileName))
                        {
                            Logger.DebugFormat("Resizing {0} - {1}", this.InputBlobId, writer.BlobId);
                            ImageResizer.Shrink(pageStream, writer.WriteStream, size.Width, size.Height);

                            CommandBus.Send(new AddFormatToDocument(
                                InputDocumentId,
                                fileFormat,
                                writer.BlobId,
                                this.PipelineId
                            ));
                        }
                    }
                }
            }

            Logger.DebugFormat("Ended resize job for {0} - {1}", this.InputBlobId, sizesAsString);
        }
    }
}
