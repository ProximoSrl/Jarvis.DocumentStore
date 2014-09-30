using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers
{
    public class AddFormatToDocumentCommandHandler : DocumentCommandHandler<AddFormatToDocument>
    {
        protected override void Execute(AddFormatToDocument cmd)
        {
            FindAndModify(cmd.AggregateId, doc => doc.AddFormat(cmd.DocumentFormat, cmd.FileId));
        }
    }
}
