using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events
{
    /// <summary>
    /// Represents a document that is marked as duplicate in the system. 
    /// The id of the domain event is the id of the <see cref="DocumentDescriptor"/>
    /// that represent the "original" descriptor. The property <see cref="DuplicatedDocumentDescriptorId"/>
    /// contains the id of the document descriptor that is duplicated.
    /// </summary>
    public class DocumentDescriptorHasBeenDeduplicated : DomainEvent
    {
        public DocumentHandleInfo HandleInfo { get; private set; }
        public DocumentDescriptorId DuplicatedDocumentDescriptorId { get; private set; }

        public DocumentDescriptorHasBeenDeduplicated(
            DocumentDescriptorId otherDocumentId, DocumentHandleInfo handleInfo)
        {
            HandleInfo = handleInfo;
            DuplicatedDocumentDescriptorId = otherDocumentId;
        }
    }
}