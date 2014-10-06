using CQRS.Shared.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Events
{
    public class DocumentCreated : DomainEvent
    {
        public FileId FileId { get; private set; }
        public FileAlias Alias { get; private set; }
        public FileNameWithExtension FileName { get; private set; }

        public DocumentCreated(DocumentId id, FileId fileId, FileAlias alias, FileNameWithExtension fileName)
        {
            FileName = fileName;
            FileId = fileId;
            Alias = alias;
            this.AggregateId = id;
        }
    }
}