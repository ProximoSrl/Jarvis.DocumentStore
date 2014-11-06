using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentQueuedForProcessing : DomainEvent
    {
        public DocumentQueuedForProcessing(BlobId blobId)
        {
            BlobId = blobId;
        }

        public BlobId BlobId { get; private set; }
    }
}