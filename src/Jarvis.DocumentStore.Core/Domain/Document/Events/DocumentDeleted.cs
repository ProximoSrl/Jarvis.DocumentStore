using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentDeleted : DomainEvent
    {
        public FileId FileId { get; private set; }
        public FileId[] FileFormatsId { get; private set; }

        public DocumentDeleted(FileId fileId, FileId[] formats)
        {
            FileId = fileId;
            FileFormatsId = formats;
        }
    }

    public class DocumentHandleDetached : DomainEvent
    {
        public DocumentHandle Handle { get; private set; }

        public DocumentHandleDetached(DocumentHandle handle)
        {
            Handle = handle;
        }
    }
}