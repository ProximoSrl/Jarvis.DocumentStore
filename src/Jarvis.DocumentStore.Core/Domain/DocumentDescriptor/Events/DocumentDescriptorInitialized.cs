using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events
{
    /// <summary>
    /// This is the event raised when a <see cref="DocumentDescriptor"/> is initialized
    /// and it is still not checked for deduplication.
    /// </summary>
    public class DocumentDescriptorInitialized : DomainEvent
    {

        public DocumentDescriptorId Id { get { return (DocumentDescriptorId)AggregateId; } }

        public BlobId BlobId { get; private set; }
        public DocumentHandleInfo HandleInfo { get; private set; }
        public FileHash Hash { get; private set; }

        /// <summary>
        /// Different from null if this <see cref="DocumentDescriptor" /> is created
        /// as attachment of another document.
        /// </summary>
        public DocumentDescriptorId FatherDocumentDescriptorId { get; private set; }

        public DocumentDescriptorInitialized(
            BlobId blobId,
            DocumentHandleInfo handleInfo,
            FileHash hash)
        {
            Hash = hash;
            HandleInfo = handleInfo;
            BlobId = blobId;
        }

        public DocumentDescriptorInitialized(
           BlobId blobId,
           DocumentHandleInfo handleInfo,
           FileHash hash,
           DocumentDescriptorId fatherDocumentDescriptorId)
        {
            Hash = hash;
            HandleInfo = handleInfo;
            BlobId = blobId;
            FatherDocumentDescriptorId = fatherDocumentDescriptorId;
        }
    }
}