using System;
using System.IO;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Processing.Pdf;
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

            Logger.DebugFormat("Conversion of {0} ({1}) in format {2} starting", InputDocumentId, InputBlobId, _format);

            var task = new CreateImageFromPdfTask { Logger = Logger };
            var descriptor = BlobStore.GetDescriptor(InputBlobId);

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

            Logger.DebugFormat("Conversion of {0} in format {1} done", InputBlobId, _format);
        }

        void Write(int pageIndex, Stream stream)
        {
            var fileName = new FileNameWithExtension(InputBlobId + ".page_" + pageIndex + "." + _format);
            var pageBlobId = BlobStore.Upload(DocumentFormats.RasterImage,fileName, stream);
            
            CommandBus.Send(
                new AddFormatToDocument(InputDocumentId, DocumentFormats.RasterImage, pageBlobId, PipelineId)
            );
        }
    }
}