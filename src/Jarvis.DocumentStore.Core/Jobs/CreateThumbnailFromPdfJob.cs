using System;
using System.IO;
using Castle.Core.Logging;
using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ProcessingPipeline.Pdf;
using Jarvis.DocumentStore.Core.Storage;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public class CreateThumbnailFromPdfJob : AbstractFileJob
    {
        FileId _fileId;
        string _format;
        DocumentId _documentId;

        public CreateThumbnailFromPdfJob(IFileStore fileStore, ICommandBus commandBus)
        {
            CommandBus = commandBus;
            FileStore = fileStore;
        }

        public ILogger Logger { get; set; }
        private IFileStore FileStore { get; set; }
        private ICommandBus CommandBus { get; set; }

        public override void Execute(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            _documentId = new DocumentId(jobDataMap.GetString(JobKeys.DocumentId));
            _fileId = new FileId(jobDataMap.GetString(JobKeys.FileId));
            _format = jobDataMap.GetString(JobKeys.FileExtension);

            Logger.DebugFormat("Conversion of {0} ({1}) in format {2} starting", _documentId, _fileId, _format);

            var task = new CreateImageFromPdfTask { Logger = Logger };
            var descriptor = FileStore.GetDescriptor(_fileId);

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

            Logger.DebugFormat("Conversion of {0} in format {1} done", _fileId, _format);
        }

        void Write(int pageIndex, Stream stream)
        {
            var pageFileId = new FileId(_fileId + ".page." + pageIndex + "." + _format);
            FileStore.Upload(pageFileId, pageFileId, stream);
            
            var fileFormat = new DocumentFormat("thumbnail");
            CommandBus.Send(
                new AddFormatToDocument(_documentId, fileFormat, pageFileId)
            );
        }
    }
}