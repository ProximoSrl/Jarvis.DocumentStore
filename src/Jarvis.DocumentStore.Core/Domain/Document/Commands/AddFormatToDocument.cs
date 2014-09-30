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
        public FormatValue Format { get; private set; }

        public AddFormatToDocument(DocumentId aggregateId, FormatValue format, FileId fileId) : base(aggregateId)
        {
            Format = format;
            FileId = fileId;
        }
    }
}
