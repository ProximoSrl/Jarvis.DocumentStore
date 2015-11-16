using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Commands;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    /// <summary>
    /// Used by document workflow to create a new handle copy of an old
    /// handle directly initialized with a specific DocumentDescriptor.
    /// </summary>
    public class CreateDocumentAsCopy : Command
    {
        public CreateDocumentAsCopy(
            DocumentHandle handle, 
            DocumentDescriptorId descriptorId,
            DocumentHandleInfo handleInfo)
        {
            Handle = handle;
            DocumentDescriptorId = descriptorId;
            HandleInfo = handleInfo;
        }

        public DocumentHandle Handle { get; private set; }

        public DocumentDescriptorId DocumentDescriptorId { get; private set; }

        public DocumentHandleInfo HandleInfo { get; private set; }
    }
}