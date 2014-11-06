using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Domain.Document;

namespace Jarvis.DocumentStore.Core.Domain.Handle.Events
{
    public class HandleLinked : DomainEvent
    {
        public DocumentId DocumentId { get; private set; }

        public HandleLinked(DocumentId documentId)
        {
            DocumentId = documentId;
        }
    }
}