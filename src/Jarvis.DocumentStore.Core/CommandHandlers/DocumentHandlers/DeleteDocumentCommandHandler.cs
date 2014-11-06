﻿using Jarvis.DocumentStore.Core.Domain.Document.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public class DeleteDocumentCommandHandler : DocumentCommandHandler<DeleteDocument>
    {
        protected override void Execute(DeleteDocument cmd)
        {
            FindAndModify(cmd.AggregateId, doc => doc.Delete(cmd.Handle));
        }
    }
}