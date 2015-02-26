using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Events;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentFileNameSet : DomainEvent
    {
        public FileNameWithExtension FileName { get; private set; }
        public DocumentHandle Handle { get; private set; }

        public DocumentFileNameSet(DocumentHandle handle, FileNameWithExtension fileName)
        {
            Handle = handle;
            FileName = fileName;
        }
    }
}