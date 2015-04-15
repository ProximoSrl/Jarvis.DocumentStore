using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands
{
    /// <summary>
    /// Create a document descriptor after we checked that it has no duplicate.
    /// </summary>
    public class CreateDocumentDescriptor : DocumentDescriptorCommand
    {


        public CreateDocumentDescriptor(DocumentDescriptorId aggregateId, DocumentHandleInfo handleInfo)
            : base(aggregateId)
        {
            HandleInfo = handleInfo;
        }

        public DocumentHandleInfo HandleInfo { get; set; }
    }
}