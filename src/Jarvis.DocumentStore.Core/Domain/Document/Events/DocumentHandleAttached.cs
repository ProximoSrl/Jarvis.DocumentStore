using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentHandleAttached : DomainEvent
    {
        public DocumentHandle Handle { get; private set; }

        public DocumentHandleAttached(DocumentHandle handle)
        {
            Handle = handle;
        }
    }
}