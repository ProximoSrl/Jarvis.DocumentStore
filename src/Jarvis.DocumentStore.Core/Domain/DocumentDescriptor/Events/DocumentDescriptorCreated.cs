using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events
{
    /// <summary>
    /// this is the event that is launched when a document has finished check for
    /// de-duplication and it was found that it is not duplicated.
    /// </summary>
    public class DocumentDescriptorCreated : DomainEvent
    {
        public DocumentDescriptorCreated(BlobId blobId, DocumentHandle handle)
        {
            BlobId = blobId;
            Handle = handle;
        }

        public BlobId BlobId { get; private set; }
        public DocumentHandle Handle { get; private set; }
    }
}