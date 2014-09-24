using System;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ProcessingPipeline.Pdf;
using Jarvis.DocumentStore.Core.Storage;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public class CreateThumbnailFromPdfJob : AbstractFileJob
    {
        public ILogger Logger { get; set; }
        public IFileStore FileStore { get; set; }

        public override void Execute(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            var fileId = new FileId(jobDataMap.GetString(JobKeys.FileId));
            var format = jobDataMap.GetString(JobKeys.FileExtension);

            var task = new CreateImageFromPdfTask();
            var descriptor = FileStore.GetDescriptor(fileId);

            using (var sourceStream = descriptor.OpenRead())
            {
                var convertParams = new CreatePdfImageTaskParams()
                {
                    Dpi = jobDataMap.GetIntOrDefault(JobKeys.Dpi, 150),
                    FromPage = jobDataMap.GetIntOrDefault(JobKeys.PagesFrom, 1),
                    Pages = jobDataMap.GetIntOrDefault(JobKeys.PagesCount, 1),
                    Format = (CreatePdfImageTaskParams.ImageFormat)Enum.Parse(typeof(CreatePdfImageTaskParams.ImageFormat), format,true)
                };

                task.Run(
                    sourceStream, 
                    convertParams,
                    (pageIndex, stream) => FileStore.Upload(fileId, "thumbnail_" + pageIndex + "."+format, stream)
                );
            }

            Logger.Debug("Task completed");
        }
    }
}