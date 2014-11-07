using CQRS.Kernel.Events;
using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.Handle.Commands;
using Jarvis.DocumentStore.Core.Domain.Handle.Events;
using Jarvis.DocumentStore.Core.Processing.Pipeline;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class DocumentWorkflow : AbstractProjection,
        IEventHandler<DocumentQueuedForProcessing>,
        IEventHandler<DocumentHasBeenDeduplicated>,
        IEventHandler<DocumentCreated>,
        IEventHandler<FormatAddedToDocument>,
        IEventHandler<HandleDeleted>
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

            _commandBus.Send(new LinkHandleToDocument(e.Handle, (DocumentId)e.AggregateId, e.OtherFileName));
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

        public void On(HandleDeleted e)
        {
            if(IsReplay)return;

            _commandBus.Send(new DeleteDocument(e.DocumentId, e.Handle));
        }
    }
}