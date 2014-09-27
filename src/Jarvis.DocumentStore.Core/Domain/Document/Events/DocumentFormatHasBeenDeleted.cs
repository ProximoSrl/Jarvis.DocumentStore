using CQRS.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentFormatHasBeenDeleted : DomainEvent
    {
        public FormatValue FormatValue { get; private set; }

        public DocumentFormatHasBeenDeleted(FormatValue formatValue)
        {
            FormatValue = formatValue;
        }
    }
}