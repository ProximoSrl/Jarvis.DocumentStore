using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class FormatAddedToDocument : DomainEvent
    {
        public FormatId FormatId { get; private set; }
        public FileId FileId { get; private set; }

        public FormatAddedToDocument(FormatId formatId, FileId fileId)
        {
            FormatId = formatId;
            FileId = fileId;
        }
    }
}