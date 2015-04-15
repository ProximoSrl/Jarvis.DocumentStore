using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Commands;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public class LinkDocumentToDocumentDescriptor : Command
    {
        public LinkDocumentToDocumentDescriptor(
            DocumentDescriptorId documentDescriptorId,
            DocumentHandleInfo handleInfo)
        {
            DocumentDescriptorId = documentDescriptorId;
            HandleInfo = handleInfo;
        }

        public DocumentDescriptorId DocumentDescriptorId { get; private set; }

        public DocumentHandleInfo HandleInfo { get; set; }
    }
}
