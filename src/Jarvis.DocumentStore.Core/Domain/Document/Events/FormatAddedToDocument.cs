using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class FormatAddedToDocument : DomainEvent
    {
        public FormatValue FormatValue { get; private set; }
        public FileId FileId { get; private set; }

        public FormatAddedToDocument(FormatValue formatValue, FileId fileId)
        {
            FormatValue = formatValue;
            FileId = fileId;
        }
    }
}