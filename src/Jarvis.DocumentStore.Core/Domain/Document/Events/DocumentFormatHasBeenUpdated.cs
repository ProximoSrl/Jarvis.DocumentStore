using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentFormatHasBeenUpdated : DomainEvent
    {
        public FormatValue FormatValue { get; private set; }
        public FileId FileId { get; private set; }

        public DocumentFormatHasBeenUpdated(FormatValue formatValue, FileId fileId)
        {
            FormatValue = formatValue;
            FileId = fileId;
        }
    }
}