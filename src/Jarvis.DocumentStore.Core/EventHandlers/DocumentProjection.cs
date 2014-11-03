using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using CQRS.Kernel.Events;
using CQRS.Kernel.ProjectionEngine;
using CQRS.Shared.Commands;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Processing.Pipeline;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using MongoDB.Driver.Builders;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class DocumentProjection : AbstractProjection,
        IEventHandler<DocumentCreated>,
        IEventHandler<FormatAddedToDocument>,
        IEventHandler<DocumentDeleted>,
        IEventHandler<DocumentHandleAttached>,
        IEventHandler<DocumentHandleDetached>,
        IEventHandler<DocumentQueuedForProcessing>,
        IEventHandler<DocumentHasBeenDeduplicated>
    {
        private readonly ICollectionWrapper<DocumentReadModel, DocumentId> _documents;
        private readonly ICollectionWrapper<HandleToDocument, DocumentHandle> _handles;
        private readonly IFileStore _fileStore;
        private readonly DeduplicationHelper _deduplicationHelper;
        private readonly ICommandBus _commandBus;
        readonly IPipelineManager _pipelineManager;

        public DocumentProjection(
            ICollectionWrapper<DocumentReadModel, DocumentId> documents, 
            ICollectionWrapper<HandleToDocument, DocumentHandle> handles,
            IFileStore fileStore, 
            DeduplicationHelper deduplicationHelper, 
            ICommandBus commandBus, 
            IPipelineManager pipelineManager
            )
        {
            _documents = documents;
            _fileStore = fileStore;
            _deduplicationHelper = deduplicationHelper;
            _commandBus = commandBus;
            _pipelineManager = pipelineManager;
            _handles = handles;

            _documents.Attach(this, false);
            _handles.Attach(this, false);

            _documents.OnSave = d =>
            {
                d.HandlesCount = d.Handles.Count;
                d.FormatsCount = d.Formats.Count;
            };
        }

        public override void Drop()
        {
            _documents.Drop();
        }

        public override void SetUp()
        {
            _documents.CreateIndex(IndexKeys<DocumentReadModel>.Ascending(x => x.Hash));
        }

        public void On(DocumentCreated e)
        {
            var descriptor = _fileStore.GetDescriptor(e.FileId);
            var document = new DocumentReadModel((DocumentId)e.AggregateId, e.FileId)
            {
                Hash = descriptor.Hash
            };

            _documents.Insert(e, document);

            if (IsReplay)
                return;

            var duplicatedId = _deduplicationHelper.FindDuplicateDocumentId(document);
            if (duplicatedId != null)
            {
                _commandBus.Send(new DeduplicateDocument(
                    duplicatedId, document.Id, e.HandleInfo 
                ));
            }
            else
            {
                _commandBus.Send(new ProcessDocument(document.Id));
            }
        }

        public void On(FormatAddedToDocument e)
        {
            _documents.FindAndModify(e, (DocumentId)e.AggregateId, d =>
            {
                d.AddFormat(e.CreatedBy, e.DocumentFormat, e.FileId);
            });

            if (IsReplay)
                return;

            var descriptor = _fileStore.GetDescriptor(e.FileId);
            Logger.DebugFormat("Next conversion step for document {0} {1}", e.FileId, descriptor.FileNameWithExtension);
            _pipelineManager.FormatAvailable(
                e.CreatedBy,
                (DocumentId)e.AggregateId, 
                e.DocumentFormat,
                descriptor
            );
        }

        public void On(DocumentDeleted e)
        {
            _documents.Delete(e, (DocumentId)e.AggregateId);
        }

        public void On(DocumentHandleDetached e)
        {
            var documentId = (DocumentId)e.AggregateId;
            _documents.FindAndModify(e, documentId, d => d.RemoveHandle(e.Handle));

            var h = _handles.FindOneById(e.Handle);
            if (h != null && h.DocumentId == documentId)
            {
                _handles.Delete(e, e.Handle);
            }
        }

        public void On(DocumentHandleAttached e)
        {
            var documentid = (DocumentId)e.AggregateId;
            _documents.FindAndModify(e, documentid, d => d.AddHandle(e.HandleInfo));

            var handle = _handles.FindOneById(e.HandleInfo.Handle);
            if (handle == null)
            {
                _handles.Insert(e, new HandleToDocument(e.HandleInfo, documentid));
            }
            else
            {
                if (!IsReplay && handle.DocumentId != documentid)
                {
                    _commandBus.Send(new DeleteDocument(handle.DocumentId, handle.Id));
                }

                handle.Link(e.HandleInfo, documentid);
                _handles.Save(e, handle);
            }
        }

        public void On(DocumentQueuedForProcessing e)
        {
            if (IsReplay) return;

            var document = _documents.FindOneById((DocumentId) e.AggregateId);
            FileId originalFileId = document.GetOriginalFileId();
            var descriptor = _fileStore.GetDescriptor(originalFileId);
            _pipelineManager.Start(document.Id, descriptor);
        }

        public void On(DocumentHasBeenDeduplicated e)
        {
            if (IsReplay)
                return;

            _commandBus.Send(new DeleteDocument(e.OtherDocumentId, e.Handle));
        }
    }

    public class DocumentByHashReader
    {
        public class Match
        {
            public DocumentId DocumentId { get; private set; }
            public FileId FileId { get; private set; }

            public Match(DocumentId documentId, FileId fileId)
            {
                DocumentId = documentId;
                FileId = fileId;
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
                .SetSnapshot()
                .Select(x => new Match(x.Id, x.GetOriginalFileId()));
        }
    }

    public class DeduplicationHelper
    {
        public ILogger Logger { get; set; }
        private readonly ConfigService _configService;
        private readonly DocumentByHashReader _hashReader;
        private readonly IFileStore _fileStore;
        public DeduplicationHelper(ConfigService configService, DocumentByHashReader hashReader, IFileStore fileStore)
        {
            _configService = configService;
            _hashReader = hashReader;
            _fileStore = fileStore;
        }

        public DocumentId FindDuplicateDocumentId(DocumentReadModel document)
        {
            if (!_configService.IsDeduplicationActive)
                return null;

            var original = _fileStore.GetDescriptor(document.GetOriginalFileId());

            var matches = _hashReader.FindDocumentByHash(document.Hash);
            Logger.DebugFormat("Deduplicating document {0}", document.Id);
            foreach (var match in matches)
            {
                if (match.DocumentId == document.Id)
                    continue;

                Logger.DebugFormat("Checking document {0}", match.DocumentId);

                var candidate = this._fileStore.GetDescriptor(match.FileId);
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
