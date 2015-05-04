using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentDescriptorHandlers
{
    public class DeleteFormatFromDocumentDescriptorCommandHandler : 
        DocumentDescriptorCommandHandler<DeleteFormatFromDocumentDescriptor>
    {
        protected override void Execute(DeleteFormatFromDocumentDescriptor cmd)
        {
            FindAndModify(cmd.AggregateId, doc => doc.DeleteFormat(cmd.DocumentFormat));
        }
    }
}
