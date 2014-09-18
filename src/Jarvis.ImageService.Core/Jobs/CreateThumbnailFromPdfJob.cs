using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Castle.Core.Interceptor;
using Castle.Core.Logging;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.ProcessingPipeline;
using Jarvis.ImageService.Core.ProcessingPipeline.Pdf;
using Jarvis.ImageService.Core.ProcessingPipeline.Tools;
using Jarvis.ImageService.Core.Services;
using Jarvis.ImageService.Core.Storage;
using Quartz;

namespace Jarvis.ImageService.Core.Jobs
{
    public class CreateThumbnailFromPdfJob : IJob
    {
        private string FileId { get; set; }
        private ImageSizeInfo[] ImageSizes { get; set; }

        public ILogger Logger { get; set; }
        public IFileStore FileStore { get; set; }
        public IImageService ImageService { get; set; }
        public ConfigService ConfigService { get; set; }

        public void Execute(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            FileId = jobDataMap.GetString(JobKeys.FileId);

            var task = new CreatePdfImageTask();
            var descriptor = FileStore.GetDescriptor(FileId);
            using (var sourceStream = descriptor.OpenRead())
            {
                var convertParams = new CreatePdfImageTaskParams()
                {
                    Dpi = jobDataMap.GetIntOrDefault(JobKeys.Dpi, 150),
                    FromPage = jobDataMap.GetIntOrDefault(JobKeys.PagesFrom, 1),
                    Pages = jobDataMap.GetIntOrDefault(JobKeys.PagesCount, 1)
                };

                var sizes = jobDataMap.GetString(JobKeys.Sizes).Split('|');

                ImageSizes = ConfigService.GetDefaultSizes().Where(x => sizes.Contains(x.Name)).ToArray();
                task.Convert(sourceStream, convertParams, SaveRasterizedPage);
            }

            Logger.DebugFormat("Deleting document {0}", FileId);

            FileStore.Delete(FileId);
            Logger.Debug("Task completed");
        }

        void SaveRasterizedPage(int i, Stream pageStream)
        {
            foreach (var size in this.ImageSizes)
            {
                pageStream.Seek(0, SeekOrigin.Begin);
                var resizeId = FileId + "/thumbnail/" + size.Name;
                Logger.DebugFormat("Writing page {0} - {1}", i, resizeId);
                using (var destStream = FileStore.CreateNew(resizeId, FileId + "." + size.Name + ".png"))
                {
                    ImageResizer.Shrink(pageStream, destStream, size.Width, size.Height);
                }
                
                ImageService.LinkImage(FileId, size.Name, resizeId);
            }
        }
    }
}