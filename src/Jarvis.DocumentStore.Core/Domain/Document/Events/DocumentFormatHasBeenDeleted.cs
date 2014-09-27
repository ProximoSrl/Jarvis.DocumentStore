using CQRS.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentFormatHasBeenDeleted : DomainEvent
    {
        public FormatId FormatId { get; private set; }

        public DocumentFormatHasBeenDeleted(FormatId formatId)
        {
            FormatId = formatId;
        }
    }
}