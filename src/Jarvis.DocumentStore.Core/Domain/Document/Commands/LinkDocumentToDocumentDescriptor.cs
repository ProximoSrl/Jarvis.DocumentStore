using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Commands;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public class LinkDocumentToDocumentDescriptor : Command
    {
        public LinkDocumentToDocumentDescriptor(DocumentHandle handle, DocumentDescriptorId documentDescriptorId)
        {
            Handle = handle;
            DocumentDescriptorId = documentDescriptorId;
        }

        public DocumentHandle Handle { get; private set; }
        public DocumentDescriptorId DocumentDescriptorId { get; private set; }
    }

    public class DeleteHandle : Command
    {
        public DeleteHandle(DocumentHandle handle)
        {
            Handle = handle;
        }

        public DocumentHandle Handle { get; private set; }
    }
}
