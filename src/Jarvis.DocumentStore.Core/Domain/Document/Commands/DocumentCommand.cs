using CQRS.Shared.Commands;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public abstract class DocumentCommand : Command<DocumentId>
    {
        protected DocumentCommand(DocumentId aggregateId) : base(aggregateId)
        {
        }
    }
}