using System.Collections.Generic;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentCreated : DomainEvent
    {
        public BlobId BlobId { get; private set; }
        public DocumentHandleInfo HandleInfo { get; private set; }
        public FileHash Hash { get; private set; }

        public DocumentHandle FatherHandle { get; private set; }

        public DocumentCreated(DocumentId id, BlobId blobId, DocumentHandleInfo handleInfo, FileHash hash)
        {
            Hash = hash;
            HandleInfo = handleInfo;
            BlobId = blobId;
            this.AggregateId = id;
        }

        public DocumentCreated(DocumentId id, BlobId blobId, DocumentHandleInfo handleInfo, DocumentHandle fatherHandle, FileHash hash)
            : this(id, blobId, handleInfo, hash)
        {
           FatherHandle = fatherHandle;
        }
    }
}