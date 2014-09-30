using System.IO;
using System.Linq;
using Castle.Core.Logging;
using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ProcessingPipeline.Tools;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public class ImageResizeJob : AbstractFileJob
    {
        public ConfigService ConfigService { get; set; }

        protected override void OnExecute(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            var fileExtension = jobDataMap.GetString(JobKeys.FileExtension);
            var sizesAsString = jobDataMap.GetString(JobKeys.Sizes);
            var sizes = sizesAsString.Split('|');

            Logger.DebugFormat("Starting resize job for {0} - {1}", this.FileId, sizesAsString);

            var imageSizes = ConfigService.GetDefaultSizes().Where(x => sizes.Contains(x.Name)).ToArray();

            var descriptor = FileStore.GetDescriptor(this.FileId);
            using (var sourceStream = descriptor.OpenRead())
            {
                using (var pageStream = new MemoryStream())
                {
                    sourceStream.CopyTo(pageStream);

                    foreach (var size in imageSizes)
                    {
                        pageStream.Seek(0, SeekOrigin.Begin);
                        var resizeId = new FileId(this.FileId + "/thumbnail/" + size.Name);
                        Logger.DebugFormat("Resizing {0} - {1}", this.FileId, resizeId);
                        var resizedImageFileName = this.FileId + "." + size.Name + "." + fileExtension;

                        using (var destStream = FileStore.CreateNew(resizeId, resizedImageFileName))
                        {
                            ImageResizer.Shrink(pageStream, destStream, size.Width, size.Height);
                        }

                        CommandBus.Send(new AddFormatToDocument(
                            DocumentId,
                            new DocumentFormat("thumb." + size.Name),
                            resizeId
                        ));
                    }
                }
            }

            Logger.DebugFormat("Ended resize job for {0} - {1}", this.FileId, sizesAsString);
        }
    }
}
