using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentHasBeenDeduplicated : DomainEvent
    {
        public DocumentId OtherDocumentId { get; set; }
        public FileAlias OtherFileAlias { get; set; }

        public DocumentHasBeenDeduplicated(DocumentId otherDocumentId, FileAlias otherFileAlias)
        {
            OtherDocumentId = otherDocumentId;
            OtherFileAlias = otherFileAlias;
        }
    }
}