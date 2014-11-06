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

        public override bool ShouldHandleFile(DocumentId documentId, IBlobDescriptor storeDescriptor, IPipeline fromPipeline)
        {
            return storeDescriptor.FileNameWithExtension.Extension == "pdf";
        }

        protected override void OnStart(DocumentId documentId, IBlobDescriptor storeDescriptor)
        {
            this.JobHelper.QueueThumbnail(Id, documentId, storeDescriptor.BlobId, ImageFormat);
        }

        protected override void OnFormatAvailable(
            DocumentId documentId, 
            DocumentFormat format, 
            IBlobDescriptor descriptor
            )
        {
            if (format == DocumentFormats.RasterImage)
            {
                this.JobHelper.QueueResize(Id, documentId, descriptor.BlobId, ImageFormat);
            }
        }
    }
}