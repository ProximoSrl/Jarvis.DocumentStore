using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events
{
    public class DocumentFormatHasBeenUpdated : DomainEvent
    {
        public DocumentFormat DocumentFormat { get; private set; }
        public BlobId BlobId { get; private set; }
        public PipelineId CreatedBy { get; private set; }

        public DocumentFormatHasBeenUpdated(DocumentFormat documentFormat, BlobId blobId, PipelineId createdBy)
        {
            DocumentFormat = documentFormat;
            BlobId = blobId;
            CreatedBy = createdBy;
        }
    }
}