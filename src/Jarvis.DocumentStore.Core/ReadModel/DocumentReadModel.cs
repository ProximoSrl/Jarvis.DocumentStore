using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.ReadModel
{
    public class DocumentReadModel : AbstractReadModel<DocumentId>
    {
        public FileId FileId { get; set; }
        public FileNameWithExtension FileName { get; set; }
        public IDictionary<DocumentFormat, FileId> Formats { get; private set; }

        public DocumentReadModel()
        {
            this.Formats = new Dictionary<DocumentFormat, FileId>();
        }

        public void AddFormat(DocumentFormat format, FileId fileId)
        {
            this.Formats[format] = fileId;
        }
    }
}
