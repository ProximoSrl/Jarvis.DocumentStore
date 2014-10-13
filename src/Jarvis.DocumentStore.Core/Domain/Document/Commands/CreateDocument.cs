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
        public FileHandle Handle { get; private set; }
        public FileNameWithExtension FileName { get; private set; }

        public CreateDocument(DocumentId documentId, FileId fileId, FileHandle handle, FileNameWithExtension fileName)
            :base(documentId)
        {
            Handle = handle;
            FileName = fileName;
            FileId = fileId;
        }
    }
}
