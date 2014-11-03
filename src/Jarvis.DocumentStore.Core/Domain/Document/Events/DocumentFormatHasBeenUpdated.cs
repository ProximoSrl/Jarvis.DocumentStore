using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
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