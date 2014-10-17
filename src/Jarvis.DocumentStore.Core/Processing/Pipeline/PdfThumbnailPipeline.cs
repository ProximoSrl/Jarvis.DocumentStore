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

        public override bool ShouldHandleFile(DocumentId documentId, IFileDescriptor descriptor)
        {
            return descriptor.FileNameWithExtension.Extension == "pdf";
        }

        protected override void OnStart(DocumentId documentId, IFileDescriptor descriptor)
        {
            this.JobHelper.QueueThumbnail(Id, documentId, descriptor.FileId, ImageFormat);
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