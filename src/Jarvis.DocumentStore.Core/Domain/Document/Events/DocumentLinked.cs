using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentLinked : DomainEvent
    {
        public DocumentDescriptorId DocumentId { get; private set; }
        public DocumentDescriptorId PreviousDocumentId { get; private set; }
        public DocumentHandle Handle { get; private set; }
        public FileNameWithExtension FileName { get; private set; }

        public DocumentLinked(DocumentHandle handle, DocumentDescriptorId documentId, DocumentDescriptorId previousDocumentId, FileNameWithExtension fileName)
        {
            FileName = fileName;
            PreviousDocumentId = previousDocumentId;
            Handle = handle;
            DocumentId = documentId;
        }
    }
}