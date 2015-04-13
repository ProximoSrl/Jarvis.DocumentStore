using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events
{
    public class DocumentDescriptorDeleted : DomainEvent
    {
        public BlobId BlobId { get; private set; }
        public BlobId[] BlobFormatsId { get; private set; }

        public DocumentHandle[] Attachments { get; set; }

        public DocumentDescriptorDeleted(
            BlobId blobId, 
            BlobId[] formats,
            DocumentHandle[] attachments)
        {
            BlobId = blobId;
            BlobFormatsId = formats;
            Attachments = attachments;
        }
    }
}