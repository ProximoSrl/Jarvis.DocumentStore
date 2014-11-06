using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Kernel.Engine;
using CQRS.Shared.Events;
using CQRS.Shared.ValueObjects;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Handle
{
    public class Handle : AggregateRoot<HandleState>
    {
        public Handle()
        {
        }

        public Handle(HandleState initialState) : base(initialState)
        {
        }

        public void Initialize(HandleId id, DocumentHandle handle)
        {
            if(HasBeenCreated)
                throw new DomainException((IIdentity)id, "handle already initialized");
            
            RaiseEvent(new HandleInitialized(id, handle));
        }

        public void Link(DocumentId documentId)
        {
            RaiseEvent(new HandleLinked(documentId));
        }
    }

    public class HandleLinked : DomainEvent
    {
        public DocumentId DocumentId { get; private set; }

        public HandleLinked(DocumentId documentId)
        {
            DocumentId = documentId;
        }
    }

    public class HandleInitialized : DomainEvent
    {
        public HandleId Id { get; private set; }
        public DocumentHandle Handle { get; private set; }

        public HandleInitialized(HandleId id, DocumentHandle handle)
        {
            Id = id;
            Handle = handle;
        }
    }

    public class HandleState : AggregateState
    {
        public HandleState(HandleId handleId, Handle handle)
        {
            this.AggregateId = handleId;
        }

        public HandleState()
        {
        }

        void When(HandleInitialized e)
        {
            this.AggregateId = e.Id;
        }
    }
}
