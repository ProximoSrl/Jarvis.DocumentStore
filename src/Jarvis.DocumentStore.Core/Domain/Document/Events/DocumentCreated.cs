using System.Collections.Generic;
using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentCreated : DomainEvent
    {
        public FileId FileId { get; private set; }
        public DocumentHandleInfo HandleInfo { get; private set; }
        public DocumentCreated(DocumentId id, FileId fileId, DocumentHandleInfo handleInfo)
        {
            HandleInfo = handleInfo;
            FileId = fileId;
            this.AggregateId = id;
        }
    }
}