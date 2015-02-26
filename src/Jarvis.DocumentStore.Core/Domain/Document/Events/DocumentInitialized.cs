using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentInitialized : DomainEvent
    {
        public DocumentId Id { get; private set; }
        public DocumentHandle Handle { get; private set; }

        public DocumentInitialized(DocumentId id, DocumentHandle handle)
        {
            Id = id;
            Handle = handle;
        }

    }
}