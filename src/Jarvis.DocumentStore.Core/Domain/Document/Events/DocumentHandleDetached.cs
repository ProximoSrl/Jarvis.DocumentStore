using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

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