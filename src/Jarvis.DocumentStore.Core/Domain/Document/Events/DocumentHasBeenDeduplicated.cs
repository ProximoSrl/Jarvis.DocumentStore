using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentHasBeenDeduplicated : DomainEvent
    {
        public DocumentId OtherDocumentId { get; private set; }
        public FileAlias OtherFileAlias { get; private set; }
        public FileNameWithExtension OtherFileName { get; private set; }

        public DocumentHasBeenDeduplicated(
            DocumentId otherDocumentId, 
            FileAlias otherFileAlias, 
            FileNameWithExtension otherFileName
        )
        {
            OtherDocumentId = otherDocumentId;
            OtherFileAlias = otherFileAlias;
            OtherFileName = otherFileName;
        }
    }
}