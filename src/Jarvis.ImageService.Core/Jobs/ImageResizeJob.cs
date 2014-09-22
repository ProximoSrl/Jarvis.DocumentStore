using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.ProcessingPipeline.Tools;
using Jarvis.ImageService.Core.Services;
using Jarvis.ImageService.Core.Storage;
using Quartz;

namespace Jarvis.ImageService.Core.Jobs
{
    public class ImageResizeJob : AbstractFileJob
    {
        public ConfigService ConfigService { get; set; }
        public IFileStore FileStore { get; set; }
        public ILogger Logger { get; set; }
        public IFileService FileService { get; set; }

        public override void Execute(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            var fileId = new FileId(jobDataMap.GetString(JobKeys.FileId));
            var fileExtension = jobDataMap.GetString(JobKeys.FileExtension);
            var sizesAsString = jobDataMap.GetString(JobKeys.Sizes);
            var sizes = sizesAsString.Split('|');

            Logger.DebugFormat("Starting resize job for {0} - {1}", fileId, sizesAsString);

            var imageSizes = ConfigService.GetDefaultSizes().Where(x => sizes.Contains(x.Name)).ToArray();

            var descriptor = FileStore.GetDescriptor(fileId);
            using (var sourceStream = descriptor.OpenRead())
            {
                using (var pageStream = new MemoryStream())
                {
                    sourceStream.CopyTo(pageStream);

                    foreach (var size in imageSizes)
                    {
                        pageStream.Seek(0, SeekOrigin.Begin);
                        var resizeId = new FileId(fileId + "/thumbnail/" + size.Name);
                        Logger.DebugFormat("Resizing {0} - {1}", fileId, resizeId);
                        var resizedImageFileName = fileId + "." + size.Name + "." + fileExtension;

                        using (var destStream = FileStore.CreateNew(resizeId, resizedImageFileName))
                        {
                            ImageResizer.Shrink(pageStream, destStream, size.Width, size.Height);
                        }

                        FileService.LinkImage(fileId, size.Name, resizeId);
                    }
                }
            }

            Logger.DebugFormat("Deleting file {0}", fileId);
            FileStore.Delete(fileId);

            Logger.DebugFormat("Ended resize job for {0} - {1}", fileId, sizesAsString);
        }
    }
}
