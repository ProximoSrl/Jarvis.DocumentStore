using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Commands;

namespace Jarvis.DocumentStore.Core.Domain.Handle.Commands
{
    public class LinkHandleToDocument : Command
    {
        public LinkHandleToDocument(DocumentHandle handle, DocumentDescriptorId documentId)
        {
            Handle = handle;
            DocumentId = documentId;
        }

        public DocumentHandle Handle { get; private set; }
        public DocumentDescriptorId DocumentId { get; private set; }
    }

    public class DeleteHandle : Command
    {
        public DeleteHandle(DocumentHandle handle)
        {
            Handle = handle;
        }

        public DocumentHandle Handle { get; private set; }
    }
}
