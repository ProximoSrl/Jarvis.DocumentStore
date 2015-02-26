using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands
{
    public class DeleteDocumentDescriptor : DocumentDescriptorCommand
    {
        public DeleteDocumentDescriptor(DocumentDescriptorId aggregateId, DocumentHandle handle) 
            : base(aggregateId)
        {
            Handle = handle;
        }

        public DocumentHandle Handle { get; private set; }
    }
}
