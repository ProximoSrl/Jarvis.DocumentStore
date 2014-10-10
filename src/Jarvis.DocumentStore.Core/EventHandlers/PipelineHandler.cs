using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using CQRS.Kernel.Events;
using CQRS.Kernel.ProjectionEngine;
using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Processing.Pipeline;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class PipelineHandler : AbstractProjection
        , IEventHandler<DocumentCreated>
        , IEventHandler<FormatAddedToDocument>
        , IEventHandler<DocumentHasBeenDeduplicated>
        , IEventHandler<DocumentDeleted>
    {
        public ILogger Logger { get; set; }
        readonly IFileStore _fileStore;
        readonly IPipelineManager _pipelineManager;
        readonly ICollectionWrapper<AliasToDocument, FileAlias> _aliasToDoc;
        readonly ICollectionWrapper<HashToDocuments, FileHash> _hashToDocs;
        readonly ICommandBus _commandBus;
        readonly ConfigService _configService;
        public PipelineHandler(IFileStore fileStore, IPipelineManager pipelineManager, ICollectionWrapper<AliasToDocument, FileAlias> aliasToDoc, ICollectionWrapper<HashToDocuments, FileHash> hashToDocs, ICommandBus commandBus, ConfigService configService)
        {
            _fileStore = fileStore;
            _pipelineManager = pipelineManager;
            _aliasToDoc = aliasToDoc;
            _hashToDocs = hashToDocs;
            _commandBus = commandBus;
            _configService = configService;

            _aliasToDoc.Attach(this, false);
            _hashToDocs.Attach(this, false);
        }

        public override void Drop()
        {
            _aliasToDoc.Drop();
            _hashToDocs.Drop();
        }

        public override void SetUp()
        {
        }

        public void On(DocumentCreated e)
        {
            var documentId = (DocumentId)e.AggregateId;

            var descriptor = _fileStore.GetDescriptor(e.FileId);
            _aliasToDoc.Upsert(e, e.Alias, CreateNewMapping(e), UpdateCurrentMapping(e));

            var hash = descriptor.Hash;
            var hashToDoc = _hashToDocs.Upsert(e, hash,
                () => new HashToDocuments(hash, documentId, e.FileId),
                m => m.LinkToDocument(documentId, e.FileId)
            );

            if (IsReplay)
                return;

            if (_configService.IsDeduplicationActive && hashToDoc.Documents.Count > 1)
            {
                foreach (var link in hashToDoc.Documents)
                {
                    // skip self
                    if (link.FileId == e.FileId)
                        continue;

                    var otherFileDescriptor = this._fileStore.GetDescriptor(link.FileId);
                    // only within same content type!
                    if (otherFileDescriptor.ContentType == descriptor.ContentType)
                    {
                        if (otherFileDescriptor.Length == descriptor.Length)
                        {
                            // binary check
                            using (var otherStream = otherFileDescriptor.OpenRead())
                            using (var thisStream = descriptor.OpenRead())
                            {
                                if (StreamsContentsAreEqual(otherStream, thisStream))
                                {
                                    Logger.DebugFormat(
                                        "Deduplicating file {0} -> {1}",
                                        e.FileId,
                                        link.FileId
                                    );

                                    // send commands to
                                    // 1 - link alias to already present document
                                    // 2 - delete "new" document
                                    _commandBus.Send(new DeduplicateDocument(link.DocumentId, documentId, e.Alias,e.FileName));
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            Logger.DebugFormat("Starting conversion of document {0} {1}", e.FileId, descriptor.FileNameWithExtension);
            _pipelineManager.Start(documentId, e.FileId);
        }

        public void On(FormatAddedToDocument e)
        {
            if (IsReplay)
                return;

            var descriptor = _fileStore.GetDescriptor(e.FileId);
            Logger.DebugFormat("Next conversion step for document {0} {1}", e.FileId, descriptor.FileNameWithExtension);
            _pipelineManager.FormatAvailable(
                e.CreatedBy,
                (DocumentId)e.AggregateId, 
                e.DocumentFormat, 
                e.FileId
            );
        }

        private Action<AliasToDocument> UpdateCurrentMapping(DocumentCreated e)
        {
            return m => m.DocumentId = (DocumentId)e.AggregateId;
        }

        private Func<AliasToDocument> CreateNewMapping(DocumentCreated e)
        {
            return () => new AliasToDocument()
            {
                DocumentId = (DocumentId)e.AggregateId
            };
        }

        /// <summary>
        /// http://stackoverflow.com/questions/1358510/how-to-compare-2-files-fast-using-net
        /// </summary>
        /// <param name="stream1"></param>
        /// <param name="stream2"></param>
        /// <returns></returns>
        private static bool StreamsContentsAreEqual(Stream stream1, Stream stream2)
        {
            const int bufferSize = 2048 * 2;
            var buffer1 = new byte[bufferSize];
            var buffer2 = new byte[bufferSize];

            while (true)
            {
                int count1 = stream1.Read(buffer1, 0, bufferSize);
                int count2 = stream2.Read(buffer2, 0, bufferSize);

                if (count1 != count2)
                {
                    return false;
                }

                if (count1 == 0)
                {
                    return true;
                }

                int iterations = (int)Math.Ceiling((double)count1 / sizeof(Int64));
                for (int i = 0; i < iterations; i++)
                {
                    if (BitConverter.ToInt64(buffer1, i * sizeof(Int64)) != BitConverter.ToInt64(buffer2, i * sizeof(Int64)))
                    {
                        return false;
                    }
                }
            }
        }

        public void On(DocumentHasBeenDeduplicated e)
        {
            _aliasToDoc.FindAndModify(e,
                e.OtherFileAlias,
                map => map.DocumentId = (DocumentId)e.AggregateId
            );

            if (IsReplay)
                return;

            _commandBus.Send(new DeleteDocument(e.OtherDocumentId));
        }

        public void On(DocumentDeleted e)
        {
            var descriptor = _fileStore.GetDescriptor(e.FileId);
            var hash = descriptor.Hash;

            _hashToDocs.FindAndModify(e, hash, m => m.UnlinkDocument((DocumentId)e.AggregateId));
        }
    }
}
