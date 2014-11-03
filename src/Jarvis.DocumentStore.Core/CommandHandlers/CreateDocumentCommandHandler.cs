using Jarvis.DocumentStore.Core.Domain.Document.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers
{
    public class CreateDocumentCommandHandler : DocumentCommandHandler<CreateDocument>
    {
        protected override void Execute(CreateDocument cmd)
        {
            FindAndModify(
                cmd.AggregateId, 
                doc => doc.Create(cmd.AggregateId,cmd.BlobId,cmd.HandleInfo),
                true
                );
        }
    }
}