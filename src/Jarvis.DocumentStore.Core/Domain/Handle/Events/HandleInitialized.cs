using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Handle.Events
{
    public class HandleInitialized : DomainEvent
    {
        public HandleId Id { get; private set; }
        public DocumentHandle Handle { get; private set; }

        public HandleId FatherId { get; private set; }

        public HandleInitialized(HandleId id, DocumentHandle handle)
        {
            Id = id;
            Handle = handle;
        }

         public HandleInitialized(HandleId id, HandleId fatherId, DocumentHandle handle)
        {
            Id = id;
            Handle = handle;
            FatherId = fatherId;
        }
    }
}