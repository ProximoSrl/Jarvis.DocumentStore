using System;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands
{
    public class DeleteFormatFromDocumentDescriptor : DocumentDescriptorCommand
    {
        public DocumentFormat DocumentFormat { get; private set; }

        public DeleteFormatFromDocumentDescriptor(
            DocumentDescriptorId aggregateId, 
            DocumentFormat documentFormat) : base(aggregateId)
        {
            if (aggregateId == null) throw new ArgumentNullException("aggregateId");
            DocumentFormat = documentFormat;
        }
    }
}
