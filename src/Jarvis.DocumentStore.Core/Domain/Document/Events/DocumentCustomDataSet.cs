using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentCustomDataSet : DomainEvent
    {
        public DocumentCustomData CustomData { get; private set; }
        public DocumentHandle Handle { get; private set; }

        public DocumentCustomDataSet(DocumentHandle handle, DocumentCustomData customData)
        {
            Handle = handle;
            CustomData = customData;
        }
    }
}