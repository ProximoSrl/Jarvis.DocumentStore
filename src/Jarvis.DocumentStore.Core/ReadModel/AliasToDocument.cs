using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.ReadModel
{
    public class AliasToDocument : AbstractReadModel<FileAlias>
    {
        public DocumentId DocumentId { get; set; }
    }
}
