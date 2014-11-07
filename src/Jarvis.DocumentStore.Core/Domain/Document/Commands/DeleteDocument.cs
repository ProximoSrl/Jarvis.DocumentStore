using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public class DeleteDocument : DocumentCommand
    {
        public DeleteDocument(
            DocumentId aggregateId, 
            DocumentHandle handle,
            string description
        ) : base(aggregateId)
        {
            Handle = handle;

            Context.Add("reason", description);
        }

        public DocumentHandle Handle { get; private set; }
    }
}
