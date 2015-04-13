using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentDescriptorHandlers
{
    public class CreateDocumentDescriptorCommandHandler : DocumentDescriptorCommandHandler<CreateDocumentDescriptor>
    {
        protected override void Execute(CreateDocumentDescriptor cmd)
        {
            FindAndModify(cmd.AggregateId, doc => doc.Create(cmd.Handle)); 
        }
    }
}