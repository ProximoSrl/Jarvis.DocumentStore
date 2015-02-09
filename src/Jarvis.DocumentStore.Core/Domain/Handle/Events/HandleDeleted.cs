using System;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Handle.Events
{
    public class HandleDeleted : DomainEvent
    {
        public HandleDeleted(DocumentHandle handle, DocumentId documentId)
        {
            if (handle == null) throw new ArgumentNullException("handle");
            if (documentId == null) throw new ArgumentNullException("documentId");
            DocumentId = documentId;
            Handle = handle;
        }

        public DocumentHandle Handle { get; private set; }
        public DocumentId DocumentId { get; private set; }
    }
}