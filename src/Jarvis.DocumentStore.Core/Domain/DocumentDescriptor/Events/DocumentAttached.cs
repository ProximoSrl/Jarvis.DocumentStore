using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events
{
    public class DocumentHandleAttached : DomainEvent
    {
        public DocumentHandle Handle { get; private set; }

        public DocumentHandleAttached(DocumentHandle handle)
        {
            Handle = handle;
        }
    }
}