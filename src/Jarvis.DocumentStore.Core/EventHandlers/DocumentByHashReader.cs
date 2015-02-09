using System.Collections.Generic;
using System.Linq;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.Framework.Shared.ReadModel;
using MongoDB.Driver.Builders;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class DocumentByHashReader
    {
        public class Match
        {
            public DocumentId DocumentId { get; private set; }
            public BlobId BlobId { get; private set; }

            public Match(DocumentId documentId, BlobId blobId)
            {
                DocumentId = documentId;
                BlobId = blobId;
            }
        }

        private IMongoDbReader<DocumentReadModel, DocumentId> _reader;

        public DocumentByHashReader(IMongoDbReader<DocumentReadModel, DocumentId> reader)
        {
            _reader = reader;
        }

        public IEnumerable<Match> FindDocumentByHash(FileHash hash)
        {
            return _reader.Collection
                .Find(Query<DocumentReadModel>.EQ(x => x.Hash, hash))
                .SetSortOrder(SortBy<DocumentReadModel>.Ascending(x=>x.SequenceNumber))
                .Select(x => new Match(x.Id, x.GetOriginalBlobId()));
        }
    }
}