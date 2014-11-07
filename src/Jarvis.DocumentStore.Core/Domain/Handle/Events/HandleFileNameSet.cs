using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Handle.Events
{
    public class HandleFileNameSet : DomainEvent
    {
        public FileNameWithExtension FileName { get; private set; }
        public DocumentHandle Handle { get; private set; }

        public HandleFileNameSet(DocumentHandle handle, FileNameWithExtension fileName)
        {
            Handle = handle;
            FileName = fileName;
        }
    }
}