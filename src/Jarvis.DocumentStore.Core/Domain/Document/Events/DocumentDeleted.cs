using CQRS.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentDeleted : DomainEvent
    {
        public DocumentDeleted()
        {
        }
    }
}