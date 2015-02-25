using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public class AddFormatToDocumentDescriptor : DocumentDescriptorCommand
    {
        public BlobId BlobId { get; private set; }
        public DocumentFormat DocumentFormat { get; private set; }
        public PipelineId CreatedBy { get; private set; }
        
        public AddFormatToDocumentDescriptor(
            DocumentDescriptorId aggregateId, 
            DocumentFormat documentFormat, 
            BlobId blobId,
            PipelineId createdById) : base(aggregateId)
        {
            if (aggregateId == null) throw new ArgumentNullException("aggregateId");
            DocumentFormat = documentFormat;
            BlobId = blobId;
            CreatedBy = createdById;
        }
    }
}
