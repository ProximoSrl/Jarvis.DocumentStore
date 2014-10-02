using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public class DeleteDocument : DocumentCommand
    {
        public DeleteDocument(DocumentId aggregateId) : base(aggregateId)
        {
        }
    }
}
