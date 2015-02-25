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
            public DocumentDescriptorId DocumentId { get; private set; }
            public BlobId BlobId { get; private set; }

            public Match(DocumentDescriptorId documentId, BlobId blobId)
            {
                DocumentId = documentId;
                BlobId = blobId;
            }
        }

        private IMongoDbReader<DocumentDescriptorReadModel, DocumentDescriptorId> _reader;

        public DocumentByHashReader(IMongoDbReader<DocumentDescriptorReadModel, DocumentDescriptorId> reader)
        {
            _reader = reader;
        }

        public IEnumerable<Match> FindDocumentByHash(FileHash hash)
        {
            return _reader.Collection
                .Find(Query<DocumentDescriptorReadModel>.EQ(x => x.Hash, hash))
                .SetSortOrder(SortBy<DocumentDescriptorReadModel>.Ascending(x=>x.SequenceNumber))
                .Select(x => new Match(x.Id, x.GetOriginalBlobId()));
        }
    }
}