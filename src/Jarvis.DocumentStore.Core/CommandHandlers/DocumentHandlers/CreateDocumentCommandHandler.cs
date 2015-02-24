using Jarvis.DocumentStore.Core.CommandHandlers.HandleHandlers;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Domain.Handle;
using Jarvis.DocumentStore.Core.ReadModel;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public class CreateDocumentCommandHandler : DocumentCommandHandler<CreateDocument>
    {
        readonly IHandleMapper _mapper;
        public CreateDocumentCommandHandler(IHandleMapper mapper, IHandleWriter writer)
        {
            _mapper = mapper;
        }

        protected override void Execute(CreateDocument cmd)
        {
            FindAndModify(
                cmd.AggregateId, 
                doc => doc.Create(cmd.AggregateId,cmd.BlobId,cmd.HandleInfo,cmd.Hash, cmd.FileName),
                true
            );

            LinkHandle(cmd);
        }

        void LinkHandle(CreateDocument cmd)
        {
            var docHandle = cmd.HandleInfo.Handle;
            var id = _mapper.Map(docHandle);
            var handle = Repository.GetById<Handle>(id);
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