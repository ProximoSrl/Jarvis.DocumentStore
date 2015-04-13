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

            LinkHandle(cmd);
        }

        void LinkHandle(InitializeDocumentDescriptor cmd)
        {
            var docHandle = cmd.HandleInfo.Handle;
            var id = _mapper.Map(docHandle);
            var handle = Repository.GetById<Document>(id);
            if (!handle.HasBeenCreated)
            {
                handle.Initialize(id, docHandle);
            }

            handle.SetFileName(cmd.FileName);
            handle.SetCustomData(cmd.HandleInfo.CustomData);
            
            Repository.Save(handle, cmd.MessageId, h => { });
        }
    }
}