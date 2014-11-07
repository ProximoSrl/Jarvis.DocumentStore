using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Handle.Events
{
    public class HandleLinked : DomainEvent
    {
        public DocumentId DocumentId { get; private set; }
        public DocumentId PreviousDocumentId { get; private set; }
        public DocumentHandle Handle { get; private set; }
        public FileNameWithExtension FileName { get; private set; }

        public HandleLinked(DocumentHandle handle, DocumentId documentId, DocumentId previousDocumentId, FileNameWithExtension fileName)
        {
            FileName = fileName;
            PreviousDocumentId = previousDocumentId;
            Handle = handle;
            DocumentId = documentId;
        }
    }
}