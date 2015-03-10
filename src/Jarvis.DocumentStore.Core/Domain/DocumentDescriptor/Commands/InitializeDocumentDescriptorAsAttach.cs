using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands
{
    public class InitializeDocumentDescriptorAsAttach : InitializeDocumentDescriptor
    {
        public DocumentHandle FatherHandle { get; private set; }

        public InitializeDocumentDescriptorAsAttach(
            DocumentDescriptorId aggregateId, 
            BlobId blobId, 
            DocumentHandleInfo handleInfo,
            DocumentHandle fatherHandle,
            FileHash hash, 
            FileNameWithExtension fileName)
            : base(aggregateId, blobId, handleInfo, hash, fileName)
        {
            FatherHandle = fatherHandle;
        }
    }
}