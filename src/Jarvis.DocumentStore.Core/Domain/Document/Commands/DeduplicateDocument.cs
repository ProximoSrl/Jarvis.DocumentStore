using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public class DeduplicateDocument : DocumentCommand
    {
        public DocumentId OtherDocumentId { get; private set; }
        public DocumentHandleInfo OtherHandleInfo { get; private set; }

        public DeduplicateDocument(DocumentId documentId, DocumentId otherDocumentId, DocumentHandleInfo otherHandleInfo)
            : base(documentId)
        {
            OtherDocumentId = otherDocumentId;
            OtherHandleInfo = otherHandleInfo;
        }
    }

    public class ProcessDocument : DocumentCommand
    {
        public ProcessDocument(DocumentId aggregateId) : base(aggregateId)
        {
        }
    }
}
