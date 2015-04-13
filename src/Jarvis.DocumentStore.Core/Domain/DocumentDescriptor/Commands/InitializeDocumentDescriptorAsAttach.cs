using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands
{
    public class InitializeDocumentDescriptorAsAttach : InitializeDocumentDescriptor
    {
        public DocumentHandle FatherHandle { get; private set; }

        public DocumentDescriptorId FatherDocumentDescriptorId { get; set; }

        public InitializeDocumentDescriptorAsAttach(
            DocumentDescriptorId aggregateId, 
            BlobId blobId, 
            DocumentHandleInfo handleInfo,
            DocumentHandle fatherHandle,
            DocumentDescriptorId fatherDocumentDescriptorId,
            FileHash hash, 
            FileNameWithExtension fileName)
            : base(aggregateId, blobId, handleInfo, hash, fileName)
        {
            FatherHandle = fatherHandle;
            FatherDocumentDescriptorId = fatherDocumentDescriptorId;
        }
    }
}