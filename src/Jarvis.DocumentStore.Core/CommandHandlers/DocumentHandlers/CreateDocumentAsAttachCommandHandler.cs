using Jarvis.DocumentStore.Core.CommandHandlers.HandleHandlers;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Domain.Handle;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using System;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public class CreateDocumentAsAttachCommandHandler : DocumentCommandHandler<CreateDocumentAsAttach>
    {
        readonly IHandleMapper _mapper;

        public CreateDocumentAsAttachCommandHandler(IHandleMapper mapper, IHandleWriter writer)
        {
            _mapper = mapper;
        }

        protected override void Execute(CreateDocumentAsAttach cmd)
        {
            FindAndModify(
                cmd.AggregateId,
                doc => doc.Create(cmd.AggregateId, cmd.BlobId, cmd.HandleInfo, cmd.Hash, cmd.FileName),
                true
            );

            LinkHandle(cmd);
        }

        void LinkHandle(CreateDocumentAsAttach cmd)
        {
            var docHandle = cmd.HandleInfo.Handle;
            var id = _mapper.Map(docHandle);
            var fatherId = _mapper.Map(cmd.FatherHandle);
            var handle = Repository.GetById<Handle>(id);
            if (!handle.HasBeenCreated)
            {
                handle.InitializeAsAttachment(id, fatherId, docHandle);
            }

            handle.SetFileName(cmd.FileName);
            handle.SetCustomData(cmd.HandleInfo.CustomData);
            
            Repository.Save(handle, cmd.MessageId, h => { });
        }
    }
}