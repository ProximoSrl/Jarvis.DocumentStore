using System.Collections.Generic;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.ReadModel
{
    public class HashToDocuments : AbstractReadModel<FileHash>
    {
        public class Link
        {
            public DocumentId DocumentId { get; private set; }
            public FileId FileId { get; private set; }

            protected internal Link(DocumentId documentId, FileId fileId)
            {
                DocumentId = documentId;
                FileId = fileId;
            }
        }

        public List<Link> Documents { get; private set; }

        protected HashToDocuments()
        {
            this.Documents = new List<Link>();
        }

        public HashToDocuments(FileHash hash, DocumentId documentId, FileId fileId)
            :this()
        {
            this.Id = hash;
            LinkToDocument(documentId, fileId);
        }

        public void LinkToDocument(DocumentId documentId, FileId fileId)
        {
            this.Documents.Add(new Link(documentId, fileId));
        }

        public void UnlinkDocument(DocumentId documentId)
        {
            this.Documents.RemoveAll(x => x.DocumentId == documentId);
        }
    }
}