using Jarvis.DocumentStore.Core.CommandHandlers.HandleHandlers;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands;
using Jarvis.DocumentStore.Core.ReadModel;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public class CreateDocumentDescriptorCommandHandler : DocumentDescriptorCommandHandler<CreateDocumentDescriptor>
    {
        readonly IHandleMapper _mapper;
        public CreateDocumentDescriptorCommandHandler(IHandleMapper mapper, IDocumentWriter writer)
        {
            _mapper = mapper;
        }

        protected override void Execute(CreateDocumentDescriptor cmd)
        {
            FindAndModify(
                cmd.AggregateId, 
                doc => doc.Create(cmd.BlobId,cmd.HandleInfo,cmd.Hash, cmd.FileName),
                true
            );

            LinkHandle(cmd);
        }

        void LinkHandle(CreateDocumentDescriptor cmd)
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