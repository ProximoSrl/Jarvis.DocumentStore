using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Handle.Commands
{
    public class LinkHandleToDocument : Command
    {
        public LinkHandleToDocument(DocumentHandle handle, DocumentId documentId, FileNameWithExtension fileName)
        {
            FileName = fileName;
            Handle = handle;
            DocumentId = documentId;
        }

        public DocumentHandle Handle { get; private set; }
        public DocumentId DocumentId { get; private set; }
        public FileNameWithExtension FileName { get; private set; }
    }
}
