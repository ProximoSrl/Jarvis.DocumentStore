using System.Collections.Generic;
using System.Linq;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.Framework.Shared.ReadModel;

using MongoDB.Driver;
using Jarvis.Framework.Shared.Helpers;
namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class DocumentDescriptorByHashReader
    {
        public class Match
        {
            public DocumentDescriptorId DocumentDescriptorId { get; private set; }
            public BlobId BlobId { get; private set; }

            public Match(DocumentDescriptorId documentDescriptorId, BlobId blobId)
            {
                DocumentDescriptorId = documentDescriptorId;
                BlobId = blobId;
            }
        }

        private IMongoDbReader<DocumentDescriptorReadModel, DocumentDescriptorId> _reader;

        public DocumentDescriptorByHashReader(IMongoDbReader<DocumentDescriptorReadModel, DocumentDescriptorId> reader)
        {
            _reader = reader;
        }

        public IEnumerable<Match> FindDocumentByHash(FileHash hash)
        {
            return _reader.Collection
                .Find(Builders<DocumentDescriptorReadModel>.Filter.Eq(x => x.Hash, hash))
                .Sort(Builders<DocumentDescriptorReadModel>.Sort.Ascending(x=>x.SequenceNumber))
                .ToList()
                .Select(x => new Match(x.Id, x.GetOriginalBlobId()));
        }
    }
}