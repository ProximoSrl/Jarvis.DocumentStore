using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events
{
    public class DocumentDescriptorCreated : DomainEvent
    {
        public BlobId BlobId { get; private set; }
        public DocumentHandleInfo HandleInfo { get; private set; }
        public FileHash Hash { get; private set; }

        public DocumentDescriptorCreated(DocumentDescriptorId id, BlobId blobId, DocumentHandleInfo handleInfo, FileHash hash)
        {
            Hash = hash;
            HandleInfo = handleInfo;
            BlobId = blobId;
            this.AggregateId = id;
        }


    }
}