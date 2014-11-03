using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class DeduplicationHelper
    {
        public ILogger Logger { get; set; }
        private readonly ConfigService _configService;
        private readonly DocumentByHashReader _hashReader;
        private readonly IBlobStore _blobStore;
        public DeduplicationHelper(ConfigService configService, DocumentByHashReader hashReader, IBlobStore blobStore)
        {
            _configService = configService;
            _hashReader = hashReader;
            _blobStore = blobStore;
        }

        public DocumentId FindDuplicateDocumentId(DocumentReadModel document)
        {
            if (!_configService.IsDeduplicationActive)
                return null;

            var original = _blobStore.GetDescriptor(document.GetOriginalBlobId());

            var matches = _hashReader.FindDocumentByHash(document.Hash);
            Logger.DebugFormat("Deduplicating document {0}", document.Id);
            foreach (var match in matches)
            {
                if (match.DocumentId == document.Id)
                    continue;

                Logger.DebugFormat("Checking document {0}", match.DocumentId);

                var candidate = this._blobStore.GetDescriptor(match.BlobId);
                // only within same content type!
                if (candidate.ContentType != original.ContentType)
                {
                    Logger.DebugFormat("document {0} has different ContentType ({1}), skipping",
                        match.DocumentId, candidate.ContentType
                        );
                    continue;
                }

                // and same length
                if (candidate.Length != original.Length)
                {
                    Logger.DebugFormat("document {0} has different length ({1}), skipping",
                        match.DocumentId, candidate.Length
                        );
                    continue;
                }
             
                // binary check
                using (var candidateStream = candidate.OpenRead())
                using (var originalStream = original.OpenRead())
                {
                    if (StreamHelper.StreamsContentsAreEqual(candidateStream, originalStream))
                    {
                        Logger.DebugFormat("{0} has same content of {1}: match found!",
                            match.DocumentId, document.Id
                            );
                        return match.DocumentId;
                    }
                    else
                    {
                        Logger.DebugFormat("{0} has different content of {1}, skipping",
                            match.DocumentId, document.Id
                            );                        
                    }
                }
            }
            return null;
        }
    }
}