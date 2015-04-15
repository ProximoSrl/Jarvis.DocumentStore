using Jarvis.DocumentStore.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands
{
    public class CreateAttachment : DocumentDescriptorCommand
    {
        public CreateAttachment(DocumentDescriptorId aggregateId, DocumentHandle handle, String attachmentPath)
            : base(aggregateId)
        {
            Handle = handle;
            AttachmentPath = attachmentPath;
        }

        public DocumentHandle Handle { get; set; }

        public String AttachmentPath { get; set; }
    }
}
