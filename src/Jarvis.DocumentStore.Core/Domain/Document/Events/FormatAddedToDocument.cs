using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class FormatAddedToDocument : DomainEvent
    {
        public DocumentFormat DocumentFormat { get; private set; }
        public FileId FileId { get; private set; }

        public FormatAddedToDocument(DocumentFormat documentFormat, FileId fileId)
        {
            DocumentFormat = documentFormat;
            FileId = fileId;
        }
    }
}