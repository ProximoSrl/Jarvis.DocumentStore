using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public class CreateDocumentAsAttach : CreateDocument
    {
        public DocumentHandle FatherHandle { get; private set; }

        public CreateDocumentAsAttach(
            DocumentId aggregateId, 
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