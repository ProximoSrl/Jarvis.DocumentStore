using CQRS.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentHandleAttached : DomainEvent
    {
        public DocumentHandleInfo HandleInfo { get; private set; }

        public DocumentHandleAttached(DocumentHandleInfo handleInfo)
        {
            HandleInfo = handleInfo;
        }
    }
}