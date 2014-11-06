using System;
using System.Text;
using System.Threading.Tasks;
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
using Jarvis.DocumentStore.Core.Storage;
using MongoDB.Driver.Builders;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class DocumentProjection : AbstractProjection,
        IEventHandler<DocumentCreated>,
        IEventHandler<FormatAddedToDocument>,
        IEventHandler<DocumentDeleted>,
        IEventHandler<DocumentQueuedForProcessing>,
        IEventHandler<DocumentHasBeenDeduplicated>
    {
        private readonly ICollectionWrapper<DocumentReadModel, DocumentId> _documents;
        private readonly IBlobStore _blobStore;
        private readonly DeduplicationHelper _deduplicationHelper;
        private readonly ICommandBus _commandBus;
        readonly IPipelineManager _pipelineManager;

        public DocumentProjection(
            ICollectionWrapper<DocumentReadModel, DocumentId> documents, 
            IBlobStore blobStore, 
            DeduplicationHelper deduplicationHelper, 
            ICommandBus commandBus, 
            IPipelineManager pipelineManager
            )
        {
            _documents = documents;
            _blobStore = blobStore;
            _deduplicationHelper = deduplicationHelper;
            _commandBus = commandBus;
            _pipelineManager = pipelineManager;

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
            var descriptor = _blobStore.GetDescriptor(e.BlobId);
            var document = new DocumentReadModel((DocumentId)e.AggregateId, e.BlobId)
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
                d.AddFormat(e.CreatedBy, e.DocumentFormat, e.BlobId);
            });

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

        public void On(DocumentDeleted e)
        {
            _documents.Delete(e, (DocumentId)e.AggregateId);
        }

        public void On(DocumentQueuedForProcessing e)
        {
            if (IsReplay) return;

            var document = _documents.FindOneById((DocumentId) e.AggregateId);
            BlobId originalBlobId = document.GetOriginalBlobId();
            var descriptor = _blobStore.GetDescriptor(originalBlobId);
            _pipelineManager.Start(document.Id, descriptor);
        }

        public void On(DocumentHasBeenDeduplicated e)
        {
            if (IsReplay)
                return;

            _commandBus.Send(new DeleteDocument(e.OtherDocumentId, e.Handle));
        }
    }
}
