using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;

namespace Jarvis.DocumentStore.Core.CommandHandlers
{
    public class DeduplicateDocumentCommandHandler : DocumentCommandHandler<DeduplicateDocument>
    {
        protected override void Execute(DeduplicateDocument cmd)
        {
           FindAndModify(cmd.AggregateId, doc => doc.Deduplicate(
               cmd.OtherDocumentId, 
               cmd.OtherHandleInfo
            )); 
        }
    }
}
