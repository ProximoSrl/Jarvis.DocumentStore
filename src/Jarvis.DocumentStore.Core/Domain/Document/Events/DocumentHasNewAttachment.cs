using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentHasNewAttachment : DomainEvent
    {
        public DocumentHasNewAttachment(DocumentHandle handle, DocumentHandle attachment)
        {
            Attachment = attachment;
            Handle = handle;
        }

        public DocumentHandle Attachment { get; private set; }

        public DocumentHandle Handle { get; private set; }
    }
}