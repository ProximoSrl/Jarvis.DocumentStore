using System;
using System.IO;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ProcessingPipeline;
using Jarvis.DocumentStore.Core.ProcessingPipeline.Pdf;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    [DisallowConcurrentExecution]
    public class CreateThumbnailFromPdfJob : AbstractFileJob
    {
        string _format;

        protected override void OnExecute(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            _format = jobDataMap.GetString(JobKeys.FileExtension);

            Logger.DebugFormat("Conversion of {0} ({1}) in format {2} starting", DocumentId, FileId, _format);

            var task = new CreateImageFromPdfTask { Logger = Logger };
            var descriptor = FileStore.GetDescriptor(FileId);

            using (var sourceStream = descriptor.OpenRead())
            {
                var convertParams = new CreatePdfImageTaskParams()
                {
                    Dpi = jobDataMap.GetIntOrDefault(JobKeys.Dpi, 150),
                    FromPage = jobDataMap.GetIntOrDefault(JobKeys.PagesFrom, 1),
                    Pages = jobDataMap.GetIntOrDefault(JobKeys.PagesCount, 1),
                    Format = (CreatePdfImageTaskParams.ImageFormat)Enum.Parse(typeof(CreatePdfImageTaskParams.ImageFormat), _format, true)
                };

                task.Run(
                    sourceStream,
                    convertParams,
                    Write
                );
            }

            Logger.DebugFormat("Conversion of {0} in format {1} done", FileId, _format);
        }

        void Write(int pageIndex, Stream stream)
        {
            var pageFileId = new FileId(FileId + ".page." + pageIndex + "." + _format);
            FileStore.Upload(pageFileId, new FileNameWithExtension(pageFileId), stream);
            
            var fileFormat = new DocumentFormat(DocumentFormats.RasterImage);
            CommandBus.Send(
                new AddFormatToDocument(DocumentId, fileFormat, pageFileId)
            );
        }
    }
}