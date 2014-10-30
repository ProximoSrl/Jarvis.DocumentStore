using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.Processing.Pipeline
{
    public class PdfThumbnailPipeline : AbstractPipeline
    {
        const string ImageFormat = "png";

        public PdfThumbnailPipeline() : base("pdf")
        {
        }

        public override bool ShouldHandleFile(DocumentId documentId, IFileStoreDescriptor storeDescriptor)
        {
            return storeDescriptor.FileNameWithExtension.Extension == "pdf";
        }

        protected override void OnStart(DocumentId documentId, IFileStoreDescriptor storeDescriptor)
        {
            this.JobHelper.QueueThumbnail(Id, documentId, storeDescriptor.FileId, ImageFormat);
        }

        protected override void OnFormatAvailable(DocumentId documentId, DocumentFormat format, FileId fileId)
        {
            if (format == DocumentFormats.RasterImage)
            {
                this.JobHelper.QueueResize(Id, documentId, fileId, ImageFormat);
            }
        }
    }
}