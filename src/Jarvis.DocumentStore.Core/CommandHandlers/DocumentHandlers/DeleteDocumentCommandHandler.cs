using Jarvis.DocumentStore.Core.Domain.Document.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public class DeleteDocumentCommandHandler : DocumentCommandHandler<DeleteDocumentDescriptor>
    {
        protected override void Execute(DeleteDocumentDescriptor cmd)
        {
            FindAndModify(cmd.AggregateId, doc => doc.Delete(cmd.Handle));
        }
    }
}
