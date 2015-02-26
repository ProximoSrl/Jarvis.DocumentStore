using System;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentDeleted : DomainEvent
    {
        public DocumentDeleted(DocumentHandle handle, DocumentDescriptorId documentDescriptorId)
        {
            if (handle == null) throw new ArgumentNullException("handle");
            if (documentDescriptorId == null) throw new ArgumentNullException("documentDescriptorId");
            DocumentDescriptorId = documentDescriptorId;
            Handle = handle;
        }

        public DocumentHandle Handle { get; private set; }
        public DocumentDescriptorId DocumentDescriptorId { get; private set; }
    }
}