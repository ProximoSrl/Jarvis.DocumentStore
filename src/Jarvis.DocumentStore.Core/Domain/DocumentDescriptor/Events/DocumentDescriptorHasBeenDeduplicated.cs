using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events
{
    public class DocumentDescriptorHasBeenDeduplicated : DomainEvent
    {
        public DocumentHandle Handle { get; private set; }
        public DocumentDescriptorId OtherDocumentId { get; private set; }
        public FileNameWithExtension OtherFileName { get; private set; }

        public DocumentDescriptorHasBeenDeduplicated(
            DocumentDescriptorId otherDocumentId, DocumentHandle handle, FileNameWithExtension otherFileName)
        {
            OtherFileName = otherFileName;
            Handle = handle;
            OtherDocumentId = otherDocumentId;
        }
    }
}