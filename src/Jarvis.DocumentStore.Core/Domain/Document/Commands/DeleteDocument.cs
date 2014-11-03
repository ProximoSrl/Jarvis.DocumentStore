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
        public DeleteDocument(DocumentId aggregateId, DocumentHandle handle) : base(aggregateId)
        {
            Handle = handle;
        }

        public DocumentHandle Handle { get; private set; }
    }

    public class CreateDocument : DocumentCommand
    {
        public FileId FileId { get; private set; }
        public DocumentHandleInfo HandleInfo { get; private set; }

        public CreateDocument(DocumentId aggregateId, FileId fileId, DocumentHandleInfo handleInfo) : base(aggregateId)
        {
            FileId = fileId;
            HandleInfo = handleInfo;
        }
    }
}
