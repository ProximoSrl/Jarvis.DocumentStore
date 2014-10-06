using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public class CreateDocument : DocumentCommand
    {
        public FileId FileId { get; private set; }
        public FileAlias Alias { get; private set; }
        public FileNameWithExtension FileName { get; private set; }

        public CreateDocument(DocumentId documentId, FileId fileId, FileAlias alias, FileNameWithExtension fileName)
            :base(documentId)
        {
            Alias = alias;
            FileName = fileName;
            FileId = fileId;
        }
    }
}
