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

            LinkHandleAndAddAttachment(cmd);

        }

        private void LinkHandleAndAddAttachment(CreateDocumentAsAttach cmd)
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

            var fatherId = _mapper.Map(cmd.FatherHandle);
            var fatherHandle = Repository.GetById<Handle>(fatherId);
            fatherHandle.AddAttachment(docHandle);

            Repository.Save(handle, cmd.MessageId, h => { });
            Repository.Save(fatherHandle, cmd.MessageId, h => { });
        }

    }
}