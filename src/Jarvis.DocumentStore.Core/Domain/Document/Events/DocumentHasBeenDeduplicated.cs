using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentHasBeenDeduplicated : DomainEvent
    {
        public DocumentId OtherDocumentId { get; private set; }
        public DocumentHandle Handle { get; private set; }

        public DocumentHasBeenDeduplicated(
            DocumentId otherDocumentId, DocumentHandle handle)
        {
            Handle = handle;
            OtherDocumentId = otherDocumentId;
        }
    }

    public class DocumentHandleAttached : DomainEvent
    {
        public DocumentHandleInfo HandleInfo { get; private set; }

        public DocumentHandleAttached(DocumentHandleInfo handleInfo)
        {
            HandleInfo = handleInfo;
        }
    }

    public class DocumentQueuedForProcessing : DomainEvent
    {
    
    }
}