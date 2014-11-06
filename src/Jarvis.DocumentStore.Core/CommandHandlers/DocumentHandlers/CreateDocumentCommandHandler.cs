using Jarvis.DocumentStore.Core.CommandHandlers.HandleHandlers;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Domain.Handle;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public class CreateDocumentCommandHandler : DocumentCommandHandler<CreateDocument>
    {
        readonly IHandleMapper _mapper;

        public CreateDocumentCommandHandler(IHandleMapper mapper)
        {
            _mapper = mapper;
        }

        protected override void Execute(CreateDocument cmd)
        {
            FindAndModify(
                cmd.AggregateId, 
                doc => doc.Create(cmd.AggregateId,cmd.BlobId,cmd.HandleInfo),
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

            handle.Link(cmd.AggregateId);

            Repository.Save(handle, cmd.MessageId, h => { });
        }
    }
}