using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentCreated : DomainEvent
    {
        public FileId FileId { get; private set; }
        public FileHandle Handle { get; private set; }
        public FileNameWithExtension FileName { get; private set; }

        public DocumentCreated(DocumentId id, FileId fileId, FileHandle handle, FileNameWithExtension fileName)
        {
            FileName = fileName;
            FileId = fileId;
            Handle = handle;
            this.AggregateId = id;
        }
    }
}