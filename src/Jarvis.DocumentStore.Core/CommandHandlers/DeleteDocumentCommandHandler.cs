using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers
{
    public class DeleteDocumentCommandHandler : DocumentCommandHandler<DeleteDocument>
    {
        protected override void Execute(DeleteDocument cmd)
        {
            FindAndModify(cmd.AggregateId, doc => doc.Delete(cmd.Handle));
        }
    }

    public class CreateDocumentCommandHandler : DocumentCommandHandler<CreateDocument>
    {
        protected override void Execute(CreateDocument cmd)
        {
            FindAndModify(
                cmd.AggregateId, 
                doc => doc.Create(cmd.AggregateId,cmd.FileId,cmd.Handle,cmd.FileName,cmd.CustomData),
                true
            );
        }
    }
}
