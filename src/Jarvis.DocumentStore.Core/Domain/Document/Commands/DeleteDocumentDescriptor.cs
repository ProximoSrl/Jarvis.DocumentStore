using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public class DeleteDocumentDescriptor : DocumentDescriptorCommand
    {
        public DeleteDocumentDescriptor(DocumentDescriptorId aggregateId, DocumentHandle handle) 
            : base(aggregateId)
        {
            Handle = handle;
        }

        public DocumentHandle Handle { get; private set; }
    }
}
