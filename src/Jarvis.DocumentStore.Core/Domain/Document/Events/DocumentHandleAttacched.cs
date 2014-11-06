using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentHandleAttacched : DomainEvent
    {
        public DocumentHandle Handle { get; private set; }

        public DocumentHandleAttacched(DocumentHandle handle)
        {
            Handle = handle;
        }
    }
}