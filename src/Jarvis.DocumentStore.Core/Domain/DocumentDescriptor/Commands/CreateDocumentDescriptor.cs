using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands
{
    public class CreateDocumentDescriptor : DocumentDescriptorCommand
    {
        public BlobId BlobId { get; private set; }
        public DocumentHandleInfo HandleInfo { get; private set; }
        public FileHash Hash { get; private set; }
        public FileNameWithExtension FileName { get; private set; }

        public CreateDocumentDescriptor(DocumentDescriptorId aggregateId, BlobId blobId, DocumentHandleInfo handleInfo, FileHash hash, FileNameWithExtension fileName) : base(aggregateId)
        {
            FileName = fileName;
            Hash = hash;
            BlobId = blobId;
            HandleInfo = handleInfo;
        }
    }
}