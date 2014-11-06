using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public class CreateDocument : DocumentCommand
    {
        public BlobId BlobId { get; private set; }
        public DocumentHandleInfo HandleInfo { get; private set; }
        public FileHash Hash { get; private set; }

        public CreateDocument(DocumentId aggregateId, BlobId blobId, DocumentHandleInfo handleInfo, FileHash hash) : base(aggregateId)
        {
            Hash = hash;
            BlobId = blobId;
            HandleInfo = handleInfo;
        }
    }
}