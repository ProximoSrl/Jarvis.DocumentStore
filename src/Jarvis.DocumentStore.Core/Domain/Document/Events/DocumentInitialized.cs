using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentInitialized : DomainEvent
    {

        public DocumentHandle Handle { get; private set; }

        public DocumentInitialized( DocumentHandle handle)
        {

            Handle = handle;
        }

    }
}