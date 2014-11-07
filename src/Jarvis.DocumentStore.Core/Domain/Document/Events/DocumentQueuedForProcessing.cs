using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentQueuedForProcessing : DomainEvent
    {
        public DocumentQueuedForProcessing(BlobId blobId, DocumentHandle handle)
        {
            BlobId = blobId;
            Handle = handle;
        }

        public BlobId BlobId { get; private set; }
        public DocumentHandle Handle { get; private set; }
    }
}