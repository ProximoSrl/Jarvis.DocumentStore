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
using Jarvis.DocumentStore.Core.Processing.Pipeline;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class ProcessingWorkflow : AbstractProjection
        , IEventHandler<DocumentCreated>
        , IEventHandler<FormatAddedToDocument>
        , IEventHandler<DocumentHandleAttached>
        , IEventHandler<DocumentHasBeenDeduplicated>
        , IEventHandler<DocumentHandleDetached>
        , IEventHandler<DocumentDeleted>
    {
        readonly IFileStore _fileStore;
        readonly IPipelineManager _pipelineManager;
        readonly ICollectionWrapper<HandleToDocument, DocumentHandle> _handleToDoc;
        readonly ICollectionWrapper<HashToDocuments, FileHash> _hashToDocs;
        readonly ICommandBus _commandBus;
        readonly ConfigService _configService;
        public ProcessingWorkflow(IFileStore fileStore, IPipelineManager pipelineManager, ICollectionWrapper<HandleToDocument, DocumentHandle> handleToDoc, ICollectionWrapper<HashToDocuments, FileHash> hashToDocs, ICommandBus commandBus, ConfigService configService)
        {
            _fileStore = fileStore;
            _pipelineManager = pipelineManager;
            _handleToDoc = handleToDoc;
            _hashToDocs = hashToDocs;
            _commandBus = commandBus;
            _configService = configService;

            _handleToDoc.Attach(this, false);
            _hashToDocs.Attach(this, false);
        }

        public override void Drop()
        {
            _handleToDoc.Drop();
            _hashToDocs.Drop();
        }

        public override void SetUp()
        {
        }

        public void On(DocumentCreated e)
        {
            Logger.DebugFormat("DocumentCreated {0}", e.Describe());
            var documentId = (DocumentId)e.AggregateId;

            _handleToDoc.Upsert(e, e.Handle, CreateNewMapping(e), UpdateCurrentMapping(e));

            var descriptor = _fileStore.GetDescriptor(e.FileId);
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
                                if (StreamHelper.StreamsContentsAreEqual(otherStream, thisStream))
                                {
                                    Logger.DebugFormat(
                                        "Deduplicating file {0} -> {1}",
                                        e.FileId,
                                        link.FileId
                                    );

                                    Logger.DebugFormat("Deduplicating {0} with {1}",documentId, link.DocumentId);
                                    _commandBus.Send(new DeduplicateDocument(link.DocumentId, documentId, e.Handle,e.FileName));
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

        private Action<HandleToDocument> UpdateCurrentMapping(DocumentCreated e)
        {
            return m =>
            {
                var aggregateId = (DocumentId) e.AggregateId;
                if (m.DocumentId != aggregateId && !IsReplay)
                {
                    _commandBus.Send(new DeleteDocument(m.DocumentId, e.Handle));
                }

                m.DocumentId = aggregateId;
                m.CustomData = e.CustomData;
            };
        }

        private Func<HandleToDocument> CreateNewMapping(DocumentCreated e)
        {
            return () => new HandleToDocument()
            {
                DocumentId = (DocumentId)e.AggregateId,
                CustomData = e.CustomData
            };
        }

        public void On(DocumentHasBeenDeduplicated e)
        {
            if (IsReplay)
                return;

            _commandBus.Send(new DeleteDocument(e.OtherDocumentId, e.Handle));
        }

        public void On(DocumentDeleted e)
        {
            var descriptor = _fileStore.GetDescriptor(e.FileId);
            var hash = descriptor.Hash;

            _hashToDocs.FindAndModify(e, hash, m => m.UnlinkDocument((DocumentId)e.AggregateId));
        }

        public void On(DocumentHandleDetached e)
        {
            var handle = _handleToDoc.FindOneById(e.Handle);
            if (handle != null && handle.DocumentId == e.AggregateId)
            {
                _handleToDoc.Delete(e, e.Handle);
            }
        }

        public void On(DocumentHandleAttached e)
        {
            _handleToDoc.FindAndModify(e,
                e.Handle,
                map => map.DocumentId = (DocumentId)e.AggregateId
            );
        }
    }
}
