using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Core.Support;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class DeduplicationHelper
    {
        public ILogger Logger { get; set; }
        private readonly DocumentStoreConfiguration _config;
        private readonly DocumentDescriptorByHashReader _hashReader;
        private readonly IBlobStore _blobStore;

        public DeduplicationHelper(
            DocumentStoreConfiguration config, 
            DocumentDescriptorByHashReader hashReader, 
            IBlobStore blobStore)
        {
            _config = config;
            _hashReader = hashReader;
            _blobStore = blobStore;
        }

        public DocumentDescriptorId FindDuplicateDocumentId(
            DocumentDescriptorId sourceDocumentId,
            FileHash sourceHash,
            BlobId sourceBlobId
            )
        {
            if (!_config.IsDeduplicationActive)
                return null;

            var original = _blobStore.GetDescriptor(sourceBlobId);

            //SUPER IMPORTANT, ORIGINAL REFERENCE FILES MUST NOT BE DE-DUPLICATED 
            if (original.IsReference)
                return null;

            var matches = _hashReader.FindDocumentByHash(sourceHash);
            Logger.DebugFormat("Deduplicating document {0}", sourceDocumentId);
            foreach (var match in matches)
            {
                if (match.DocumentDescriptorId == sourceDocumentId)
                    continue;

                Logger.DebugFormat("Checking document {0}", match.DocumentDescriptorId);

                var candidate = this._blobStore.GetDescriptor(match.BlobId);
                // only within same content type!
                if (candidate.ContentType != original.ContentType)
                {
                    Logger.DebugFormat("document {0} has different ContentType ({1}), skipping",
                        match.DocumentDescriptorId, candidate.ContentType
                        );
                    continue;
                }

                // and same length
                if (candidate.Length != original.Length)
                {
                    Logger.DebugFormat("document {0} has different length ({1}), skipping",
                        match.DocumentDescriptorId, candidate.Length
                        );
                    continue;
                }

                // only within same content type!
                if (candidate.IsReference)
                {
                    Logger.DebugFormat("document {0} Is a reference ({1}), cannot be used to de-duplicate",
                        match.DocumentDescriptorId, candidate.ContentType
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
                            match.DocumentDescriptorId, sourceDocumentId
                            );
                        return match.DocumentDescriptorId;
                    }
                    else
                    {
                        Logger.DebugFormat("{0} has different content of {1}, skipping",
                            match.DocumentDescriptorId, sourceDocumentId
                            );                        
                    }
                }
            }
            return null;
        }
    }
}