using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public class CreateDocumentDescriptorAsAttach : CreateDocumentDescriptor
    {
        public DocumentHandle FatherHandle { get; private set; }

        public CreateDocumentDescriptorAsAttach(
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