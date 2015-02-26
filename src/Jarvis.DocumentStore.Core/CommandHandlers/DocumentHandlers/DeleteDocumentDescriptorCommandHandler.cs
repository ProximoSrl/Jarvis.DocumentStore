using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public class DeleteDocumentDescriptorCommandHandler : DocumentDescriptorCommandHandler<DeleteDocumentDescriptor>
    {
        protected override void Execute(DeleteDocumentDescriptor cmd)
        {
            FindAndModify(cmd.AggregateId, doc => doc.Delete(cmd.Handle));
        }
    }
}
