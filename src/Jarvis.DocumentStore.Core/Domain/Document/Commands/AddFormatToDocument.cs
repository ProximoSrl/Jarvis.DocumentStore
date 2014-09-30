using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public class AddFormatToDocument : DocumentCommand
    {
        public FileId FileId { get; private set; }
        public DocumentFormat DocumentFormat { get; private set; }

        public AddFormatToDocument(DocumentId aggregateId, DocumentFormat documentFormat, FileId fileId) : base(aggregateId)
        {
            DocumentFormat = documentFormat;
            FileId = fileId;
        }
    }
}
