using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

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