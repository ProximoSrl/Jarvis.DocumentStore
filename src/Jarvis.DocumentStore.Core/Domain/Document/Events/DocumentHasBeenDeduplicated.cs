using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentHasBeenDeduplicated : DomainEvent
    {
        public DocumentId OtherDocumentId { get; private set; }
        public DocumentHandle OtherDocumentHandle { get; private set; }
        public FileNameWithExtension OtherFileName { get; private set; }

        public DocumentHasBeenDeduplicated(
            DocumentId otherDocumentId, 
            DocumentHandle otherDocumentHandle, 
            FileNameWithExtension otherFileName
        )
        {
            OtherDocumentId = otherDocumentId;
            OtherDocumentHandle = otherDocumentHandle;
            OtherFileName = otherFileName;
        }
    }
}