using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands
{
    public class DeduplicateDocumentDescriptor : DocumentDescriptorCommand
    {
        public DocumentDescriptorId OtherDocumentId { get; private set; }
        public DocumentHandle OtherHandle { get; private set; }
        public FileNameWithExtension OtherFileName { get; private set; }

        public DeduplicateDocumentDescriptor(DocumentDescriptorId documentId, DocumentDescriptorId otherDocumentId, DocumentHandle otherHandle)
            : base(documentId)
        {
            OtherDocumentId = otherDocumentId;
            OtherHandle = otherHandle;
        }
    }
}
