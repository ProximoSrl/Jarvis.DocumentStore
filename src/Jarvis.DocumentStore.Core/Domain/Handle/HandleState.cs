using CQRS.Kernel.Engine;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Handle.Events;

namespace Jarvis.DocumentStore.Core.Domain.Handle
{
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