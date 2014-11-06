using CQRS.Kernel.Engine;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Handle.Events;
using Jarvis.DocumentStore.Core.Model;

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
            this.Handle = e.Handle;
        }

        void When(HandleDeleted e)
        {
            MarkAsDeleted();
        }

        void When(HandleCustomDataSet e)
        {
            this.CustomData = e.CustomData;
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
        public HandleCustomData CustomData { get; private set; }
        public DocumentHandle Handle { get; private set; }

        public void SetCustomData(HandleCustomData data)
        {
            this.CustomData = data;
        }
    }
}