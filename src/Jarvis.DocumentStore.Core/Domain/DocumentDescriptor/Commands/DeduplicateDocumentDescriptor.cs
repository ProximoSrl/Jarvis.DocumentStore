using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands
{
    public class DeduplicateDocumentDescriptor : DocumentDescriptorCommand
    {
        public DocumentDescriptorId OtherDocumentDescriptorId { get; private set; }

        public DocumentHandle OtherHandle { get; private set; }

        public FileNameWithExtension OtherFileName { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="documentDescriptorId">The id of the original <see cref="DocumentDescriptorId"/>, this means
        /// that the document with id <param name="otherDocumentDescriptorId"> will be linked to this descriptor</param></param>
        /// <param name="otherDocumentDescriptorId">The id of DocumentDescriptor that is de-duplicated</param>
        /// <param name="otherHandle"></param>
        /// <param name="otherFileName"></param>
        public DeduplicateDocumentDescriptor(
            DocumentDescriptorId documentDescriptorId, 
            DocumentDescriptorId otherDocumentDescriptorId, 
            DocumentHandle otherHandle,
            FileNameWithExtension otherFileName)
            : base(documentDescriptorId)
        {
            OtherDocumentDescriptorId = otherDocumentDescriptorId;
            OtherHandle = otherHandle;
            OtherFileName = otherFileName;
        }
    }
}
