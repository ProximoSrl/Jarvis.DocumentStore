using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Handle.Events
{
    public class HandleDeleted : DomainEvent
    {
        public HandleDeleted(DocumentHandle handle)
        {
            Handle = handle;
        }

        public DocumentHandle Handle { get; private set; }
    }
}