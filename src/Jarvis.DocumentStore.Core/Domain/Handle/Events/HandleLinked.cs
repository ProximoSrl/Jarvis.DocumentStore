using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Handle.Events
{
    public class HandleLinked : DomainEvent
    {
        public DocumentDescriptorId DocumentId { get; private set; }
        public DocumentDescriptorId PreviousDocumentId { get; private set; }
        public DocumentHandle Handle { get; private set; }
        public FileNameWithExtension FileName { get; private set; }

        public HandleLinked(DocumentHandle handle, DocumentDescriptorId documentId, DocumentDescriptorId previousDocumentId, FileNameWithExtension fileName)
        {
            FileName = fileName;
            PreviousDocumentId = previousDocumentId;
            Handle = handle;
            DocumentId = documentId;
        }
    }
}