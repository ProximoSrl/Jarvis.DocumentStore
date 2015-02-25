using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public class DeduplicateDocumentCommandHandler : DocumentCommandHandler<DeduplicateDocumentDescriptor>
    {
        protected override void Execute(DeduplicateDocumentDescriptor cmd)
        {
           FindAndModify(cmd.AggregateId, doc => doc.Deduplicate(
               cmd.OtherDocumentId, 
               cmd.OtherHandle,
               cmd.OtherFileName
            )); 
        }
    }
}
