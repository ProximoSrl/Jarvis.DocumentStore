using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public class CreateDocument : DocumentCommand
    {
        public BlobId BlobId { get; private set; }
        public DocumentHandleInfo HandleInfo { get; private set; }

        public CreateDocument(DocumentId aggregateId, BlobId blobId, DocumentHandleInfo handleInfo) : base(aggregateId)
        {
            BlobId = blobId;
            HandleInfo = handleInfo;
        }
    }
}