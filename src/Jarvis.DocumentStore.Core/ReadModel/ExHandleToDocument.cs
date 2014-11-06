using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.ReadModel
{
    public class ExHandleToDocument : AbstractReadModel<DocumentHandle>
    {
        public ExHandleToDocument(DocumentHandleInfo handleInfo, DocumentId documentid)
        {
            this.Id = handleInfo.Handle;
            Link(handleInfo, documentid);
        }

        public DocumentId DocumentId { get; set; }
        public IDictionary<string, object> CustomData { get; set; }

        public void Link(DocumentHandleInfo handleInfo, DocumentId documentid)
        {
            this.DocumentId = documentid;
            this.CustomData = handleInfo.CustomData;
        }
    }
}
