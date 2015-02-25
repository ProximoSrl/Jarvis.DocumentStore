
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Handle.Events
{
    public class HandleHasNewAttachment : DomainEvent
    {
        public HandleHasNewAttachment(DocumentHandle handle, DocumentHandle attachment)
        {
            Attachment = attachment;
            Handle = handle;
        }

        public DocumentHandle Attachment { get; private set; }

        public DocumentHandle Handle { get; private set; }
    }
}