using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Core.Services
{
    public class FileHandleMapper : IFileHandleMapper
    {
        internal class HandleToDocumentId
        {
            public FileHandle Id { get; private set; }
            public DocumentId DocumentId { get; private set; }

            public HandleToDocumentId(FileHandle id, DocumentId documentId)
            {
                Id = id;
                DocumentId = documentId;
            }
        }

        readonly MongoCollection<HandleToDocumentId> _handles;

        public FileHandleMapper(MongoDatabase db)
        {
            _handles = db.GetCollection<HandleToDocumentId>("map_handles");
        }

        public void Associate(FileHandle handle, DocumentId documentId)
        {
            _handles.Save(new HandleToDocumentId(handle, documentId));
        }
    }
}