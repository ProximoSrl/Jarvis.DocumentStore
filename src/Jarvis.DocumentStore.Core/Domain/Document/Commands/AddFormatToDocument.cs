using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public class AddFormatToDocument : DocumentCommand
    {
        public BlobId BlobId { get; private set; }
        public DocumentFormat DocumentFormat { get; private set; }
        public PipelineId CreatedBy { get; private set; }
        
        public AddFormatToDocument(
            DocumentId aggregateId, 
            DocumentFormat documentFormat, 
            BlobId blobId,
            PipelineId createdById) : base(aggregateId)
        {
            DocumentFormat = documentFormat;
            BlobId = blobId;
            CreatedBy = createdById;
        }
    }
}
