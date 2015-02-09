using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentHasBeenDeduplicated : DomainEvent
    {
        public DocumentHandle Handle { get; private set; }
        public DocumentId OtherDocumentId { get; private set; }
        public FileNameWithExtension OtherFileName { get; private set; }

        public DocumentHasBeenDeduplicated(
            DocumentId otherDocumentId, DocumentHandle handle, FileNameWithExtension otherFileName)
        {
            OtherFileName = otherFileName;
            Handle = handle;
            OtherDocumentId = otherDocumentId;
        }
    }
}