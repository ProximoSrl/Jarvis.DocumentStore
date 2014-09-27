using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Shared.Commands;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public class CreateDocument : Command<DocumentId>
    {
    }
}
