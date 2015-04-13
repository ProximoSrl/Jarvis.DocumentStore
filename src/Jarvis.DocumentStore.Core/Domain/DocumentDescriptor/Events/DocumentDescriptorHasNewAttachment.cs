using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events
{
    public class DocumentDescriptorHasNewAttachment: DomainEvent
    {
        public DocumentDescriptorHasNewAttachment(DocumentHandle attachment, String attachmentPath)
        {
            Attachment = attachment;
            AttachmentPath = attachmentPath;
        }

        public String AttachmentPath { get; set; }

        public DocumentHandle Attachment { get; private set; }

    }
}
