using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentHasBeenDeduplicated : DomainEvent
    {
        public DocumentId OtherDocumentId { get; private set; }
        public FileHandle OtherFileHandle { get; private set; }
        public FileNameWithExtension OtherFileName { get; private set; }

        public DocumentHasBeenDeduplicated(
            DocumentId otherDocumentId, 
            FileHandle otherFileHandle, 
            FileNameWithExtension otherFileName
        )
        {
            OtherDocumentId = otherDocumentId;
            OtherFileHandle = otherFileHandle;
            OtherFileName = otherFileName;
        }
    }
}