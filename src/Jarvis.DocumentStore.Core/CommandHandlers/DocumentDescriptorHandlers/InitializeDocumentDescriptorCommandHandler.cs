using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentDescriptorHandlers
{
    public class InitializeDocumentDescriptorCommandHandler : DocumentDescriptorCommandHandler<InitializeDocumentDescriptor>
    {
        protected override void Execute(InitializeDocumentDescriptor cmd)
        {
            FindAndModify(
                cmd.AggregateId,
                doc => {
                    if (!doc.HasBeenCreated)
                    {
                        doc.Initialize(cmd.BlobId, cmd.HandleInfo, cmd.Hash, cmd.FileName);
                    }
                },
                true
            );
        }
    }
}