using System;
using System.Text;
using System.Threading.Tasks;
using CQRS.Kernel.Events;
using CQRS.Kernel.ProjectionEngine;
using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Processing.Pipeline;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Storage;
using MongoDB.Driver.Builders;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class DocumentProjection : AbstractProjection,
        IEventHandler<DocumentCreated>,
        IEventHandler<FormatAddedToDocument>,
        IEventHandler<DocumentDeleted>
    {
        private readonly ICollectionWrapper<DocumentReadModel, DocumentId> _documents;

        public DocumentProjection(
            ICollectionWrapper<DocumentReadModel, DocumentId> documents
        )
        {
            _documents = documents;

            _documents.Attach(this, false);

            _documents.OnSave = d =>
            {
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
            var document = new DocumentReadModel((DocumentId)e.AggregateId, e.BlobId)
            {
                Hash = e.Hash
            };

            _documents.Insert(e, document);
        }

        public void On(FormatAddedToDocument e)
        {
            _documents.FindAndModify(e, (DocumentId)e.AggregateId, d =>
            {
                d.AddFormat(e.CreatedBy, e.DocumentFormat, e.BlobId);
            });
        }

        public void On(DocumentDeleted e)
        {
            _documents.Delete(e, (DocumentId)e.AggregateId);
        }
    }

    public class DocumentWorkflow : AbstractProjection,
        IEventHandler<DocumentQueuedForProcessing>,
        IEventHandler<DocumentHasBeenDeduplicated>,
        IEventHandler<DocumentCreated>,
        IEventHandler<FormatAddedToDocument>
    {
        private readonly ICommandBus _commandBus;
        private readonly IBlobStore _blobStore;
        private readonly DeduplicationHelper _deduplicationHelper;
        readonly IPipelineManager _pipelineManager;

        public DocumentWorkflow(ICommandBus commandBus, IBlobStore blobStore, DeduplicationHelper deduplicationHelper, IPipelineManager pipelineManager)
        {
            _commandBus = commandBus;
            _blobStore = blobStore;
            _deduplicationHelper = deduplicationHelper;
            _pipelineManager = pipelineManager;
        }

        public override void Drop()
        {
            
        }

        public override void SetUp()
        {
        }

        public void On(DocumentQueuedForProcessing e)
        {
            if (IsReplay) return;

            var descriptor = _blobStore.GetDescriptor(e.BlobId);
            _pipelineManager.Start((DocumentId) e.AggregateId, descriptor, null);
        }

        public void On(DocumentHasBeenDeduplicated e)
        {
            if (IsReplay)
                return;

            _commandBus.Send(new DeleteDocument(e.OtherDocumentId, e.Handle));
        }
        public void On(FormatAddedToDocument e)
        {
            if (IsReplay)
                return;

            var descriptor = _blobStore.GetDescriptor(e.BlobId);
            Logger.DebugFormat("Next conversion step for document {0} {1}", e.BlobId, descriptor.FileNameWithExtension);
            _pipelineManager.FormatAvailable(
                e.CreatedBy,
                (DocumentId)e.AggregateId,
                e.DocumentFormat,
                descriptor
            );
        }

        public void On(DocumentCreated e)
        {
            if (IsReplay)
                return;

            var thisDocumentId = (DocumentId) e.AggregateId;

            var duplicatedId = _deduplicationHelper.FindDuplicateDocumentId(
                thisDocumentId,
                e.Hash,
                e.BlobId
            );

            if (duplicatedId != null)
            {
                _commandBus.Send(new DeduplicateDocument(
                    duplicatedId, thisDocumentId, e.HandleInfo.Handle
                ));
            }
            else
            {
                _commandBus.Send(new ProcessDocument(thisDocumentId));
            }
        }
    }
}
