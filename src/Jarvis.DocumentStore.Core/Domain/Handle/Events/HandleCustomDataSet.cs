using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Handle.Events
{
    public class HandleCustomDataSet : DomainEvent
    {
        public HandleCustomData CustomData { get; private set; }
        public DocumentHandle Handle { get; private set; }

        public HandleCustomDataSet(DocumentHandle handle, HandleCustomData customData)
        {
            Handle = handle;
            CustomData = customData;
        }
    }
}