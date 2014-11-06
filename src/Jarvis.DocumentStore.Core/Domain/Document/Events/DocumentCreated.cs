using System.Collections.Generic;
using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentCreated : DomainEvent
    {
        public BlobId BlobId { get; private set; }
        public DocumentHandleInfo HandleInfo { get; private set; }
        public FileHash Hash { get; private set; }

        public DocumentCreated(DocumentId id, BlobId blobId, DocumentHandleInfo handleInfo, FileHash hash)
        {
            Hash = hash;
            HandleInfo = handleInfo;
            BlobId = blobId;
            this.AggregateId = id;
        }
    }
}