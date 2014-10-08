using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Core.Services
{
    public class FileAliasMapper : IFileAliasMapper
    {
        internal class AliasToDocumentId
        {
            public FileAlias Id { get; private set; }
            public DocumentId DocumentId { get; private set; }

            public AliasToDocumentId(FileAlias id, DocumentId documentId)
            {
                Id = id;
                DocumentId = documentId;
            }
        }

        readonly MongoCollection<AliasToDocumentId> _aliases;

        public FileAliasMapper(MongoDatabase db)
        {
            _aliases = db.GetCollection<AliasToDocumentId>("map_alias");
        }

        public void Associate(FileAlias alias, DocumentId documentId)
        {
            _aliases.Save(new AliasToDocumentId(alias, documentId));
        }
    }
}