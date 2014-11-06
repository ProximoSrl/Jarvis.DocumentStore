using CQRS.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Handle.Events
{
    public class HandleCustomDataSet : DomainEvent
    {
        public HandleCustomData CustomData { get; private set; }

        public HandleCustomDataSet(HandleCustomData customData)
        {
            CustomData = customData;
        }
    }
}