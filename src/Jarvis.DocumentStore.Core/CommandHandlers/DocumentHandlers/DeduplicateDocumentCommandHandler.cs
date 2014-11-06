using Jarvis.DocumentStore.Core.Domain.Document.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public class DeduplicateDocumentCommandHandler : DocumentCommandHandler<DeduplicateDocument>
    {
        protected override void Execute(DeduplicateDocument cmd)
        {
           FindAndModify(cmd.AggregateId, doc => doc.Deduplicate(
               cmd.OtherDocumentId, 
               cmd.OtherHandle
            )); 
        }
    }
}
