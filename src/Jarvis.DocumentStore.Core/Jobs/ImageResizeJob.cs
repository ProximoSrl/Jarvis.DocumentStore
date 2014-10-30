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
            var jobDataMap = context.JobDetail.JobDataMap;
            var fileExtension = jobDataMap.GetString(JobKeys.FileExtension);
            var sizesAsString = jobDataMap.GetString(JobKeys.Sizes);
            var sizes = sizesAsString.Split('|');

            Logger.DebugFormat("Starting resize job for {0} - {1}", this.FileId, sizesAsString);

            var imageSizes = ConfigService.GetDefaultSizes().Where(x => sizes.Contains(x.Name)).ToArray();

            var descriptor = FileStore.GetDescriptor(this.FileId);

            var fileIdName = new FileNameWithExtension(this.FileId);

            using (var sourceStream = descriptor.OpenRead())
            {
                using (var pageStream = new MemoryStream())
                {
                    sourceStream.CopyTo(pageStream);

                    foreach (var size in imageSizes)
                    {
                        pageStream.Seek(0, SeekOrigin.Begin);
                        var resizeId = new FileId(fileIdName.FileName + "." + size.Name+"."+fileIdName.Extension);
                        Logger.DebugFormat("Resizing {0} - {1}", this.FileId, resizeId);
                        var resizedImageFileName = new FileNameWithExtension(this.FileId + "." + size.Name + "." + fileExtension);

                        using (var writer = FileStore.CreateNew(resizedImageFileName))
                        {
                            ImageResizer.Shrink(pageStream, writer.WriteStream, size.Width, size.Height);
                        }

                        CommandBus.Send(new AddFormatToDocument(
                            DocumentId,
                            new DocumentFormat("thumb." + size.Name),
                            resizeId,
                            this.PipelineId
                        ));
                    }
                }
            }

            Logger.DebugFormat("Ended resize job for {0} - {1}", this.FileId, sizesAsString);
        }
    }
}
