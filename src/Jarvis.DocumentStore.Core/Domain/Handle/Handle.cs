﻿using System;
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
            ThrowIfDeleted();
            
            RaiseEvent(new HandleInitialized(id, handle));
        }

        public void Link(DocumentId documentId)
        {
            ThrowIfDeleted();
            if(InternalState.LinkedDocument != documentId)
                RaiseEvent(new HandleLinked(documentId));
        }

        public void Delete()
        {
            if(!InternalState.HasBeenDeleted)
                RaiseEvent(new HandleDeleted());
        }

        void ThrowIfDeleted()
        {
            if(InternalState.HasBeenDeleted)
                throw new DomainException((IIdentity)Id, "Handle has been deleted");
        }
    }

    public class HandleDeleted : DomainEvent
    {
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

        void When(HandleDeleted e)
        {
            MarkAsDeleted();
        }

        public void Link(DocumentId documentId)
        {
            this.LinkedDocument = documentId;
        }

        public DocumentId LinkedDocument { get; private set; }

        public void MarkAsDeleted()
        {
            this.HasBeenDeleted = true;
        }

        public bool HasBeenDeleted { get; private set; }
    }
}