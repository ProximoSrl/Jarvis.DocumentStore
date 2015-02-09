using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentDeleted : DomainEvent
    {
        public BlobId BlobId { get; private set; }
        public BlobId[] BlobFormatsId { get; private set; }

        public DocumentDeleted(BlobId blobId, BlobId[] formats)
        {
            BlobId = blobId;
            BlobFormatsId = formats;
        }
    }
}