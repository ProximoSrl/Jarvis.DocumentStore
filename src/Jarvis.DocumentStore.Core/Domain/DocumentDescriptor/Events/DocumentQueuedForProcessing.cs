using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events
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