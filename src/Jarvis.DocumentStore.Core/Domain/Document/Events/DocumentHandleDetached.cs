using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentHandleDetached : DomainEvent
    {
        public DocumentHandle Handle { get; private set; }

        public DocumentHandleDetached(DocumentHandle handle)
        {
            Handle = handle;
        }
    }
}