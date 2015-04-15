using Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands;
using Jarvis.DocumentStore.Core.ReadModel;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentDescriptorHandlers
{
    public class InitializeDocumentDescriptorCommandHandler : DocumentDescriptorCommandHandler<InitializeDocumentDescriptor>
    {
        readonly IHandleMapper _mapper;
        public InitializeDocumentDescriptorCommandHandler(IHandleMapper mapper, IDocumentWriter writer)
        {
            _mapper = mapper;
        }

        protected override void Execute(InitializeDocumentDescriptor cmd)
        {
            FindAndModify(
                cmd.AggregateId, 
                doc => {
                    if (!doc.HasBeenCreated)
                        doc.Initialize(cmd.BlobId, cmd.HandleInfo, cmd.Hash, cmd.FileName);
                },
                true
            );
        }

     
    }
}