using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Kernel.Engine;
using CQRS.Shared.Events;
using CQRS.Shared.IdentitySupport;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    public class Document : AggregateRoot<DocumentState>
    {
        public void Create(DocumentId id)
        {
            RaiseEvent(new DocumentCreated(id));
        }
    }

    public class DocumentCreated : DomainEvent
    {
        public DocumentCreated(DocumentId id)
        {
            this.AggregateId = id;
        }
    }

    public class DocumentState : AggregateState
    {
        void When(DocumentCreated e)
        {
            this.AggregateId = e.AggregateId;
        }
    }

    public class DocumentId : EventStoreIdentity
    {
        public DocumentId(string id) : base(id)
        {
        }

        public DocumentId(long id) : base(id)
        {
        }
    }
}
