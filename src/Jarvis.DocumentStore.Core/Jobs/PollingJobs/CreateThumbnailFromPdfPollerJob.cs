using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Processing.Pdf;
using Jarvis.DocumentStore.Core.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Jobs.PollingJobs
{
    public class CreateThumbnailFromPdfPollerJob : AbstractPollerFileJob
    {
        public CreateThumbnailFromPdfPollerJob()
        {
            base.PipelineId = new PipelineId("pdf");
            base.QueueName = "pdfThumb";
        }

        protected override void OnPolling(PollerJobBaseParameters baseParameters, IDictionary<string, string> fullParameters, Storage.IBlobStore currentTenantBlobStore, string workingFolder)
        {
            String format = fullParameters[JobKeys.ThumbnailFormat];

            Logger.DebugFormat("Conversion of {0} ({1}) in format {2} starting", baseParameters.InputDocumentId, baseParameters.InputBlobId, format);

            var task = new CreateImageFromPdfTask { Logger = Logger };
            var descriptor = currentTenantBlobStore.GetDescriptor(baseParameters.InputBlobId);

            using (var sourceStream = descriptor.OpenRead())
            {
                var convertParams = new CreatePdfImageTaskParams()
                {
                    
                    Dpi = fullParameters.GetIntOrDefault(JobKeys.Dpi, 150),
                    FromPage = fullParameters.GetIntOrDefault(JobKeys.PagesFrom, 1),
                    Pages = fullParameters.GetIntOrDefault(JobKeys.PagesCount, 1),
                    Format = (CreatePdfImageTaskParams.ImageFormat)Enum.Parse(typeof(CreatePdfImageTaskParams.ImageFormat), format, true)
                };

                PageWriter pageWriter = new PageWriter()
                {
                    BaseParameters = baseParameters,
                    BlobStore = currentTenantBlobStore,
                    CommandBus = this.CommandBus,
                    Format = format,
                    PipelineId = this.PipelineId,
                };

                task.Run(
                    sourceStream,
                    convertParams,
                    pageWriter.Write
                );
            }

            Logger.DebugFormat("Conversion of {0} in format {1} done", baseParameters.InputBlobId, format);
        }

        private class PageWriter 
        {
            public PollerJobBaseParameters BaseParameters { get; set; }

            public IBlobStore BlobStore { get; set; }

            public String Format { get; set; }

            public ICommandBus CommandBus { get; set; }

            public PipelineId PipelineId { get; set; }

            public void Write(int pageIndex, Stream stream)
            {
                var fileName = new FileNameWithExtension(BaseParameters.InputBlobId + ".page_" + pageIndex + "." + Format);
                var pageBlobId = BlobStore.Upload(DocumentFormats.RasterImage, fileName, stream);

                CommandBus.Send(
                    new AddFormatToDocument(BaseParameters.InputDocumentId, DocumentFormats.RasterImage, pageBlobId, PipelineId)
                );
            }

        }
        

       
    }
}
