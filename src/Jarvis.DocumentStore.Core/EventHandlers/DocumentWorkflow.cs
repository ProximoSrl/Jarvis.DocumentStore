using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Commands;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Shared.Jobs;
using Jarvis.Framework.Kernel.Events;
using Jarvis.Framework.Kernel.ProjectionEngine;
using Jarvis.Framework.Shared.Commands;
using System;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class DocumentWorkflow : AbstractProjection,
        IEventHandler<DocumentDescriptorCreated>,
        IEventHandler<DocumentDescriptorHasBeenDeduplicated>,
        IEventHandler<DocumentDescriptorInitialized>,
        IEventHandler<FormatAddedToDocumentDescriptor>,
        IEventHandler<DocumentDeleted>,
        IEventHandler<DocumentDescriptorDeleted>,
        IEventHandler<DocumentLinked>
    {
        private readonly ICommandBus _commandBus;
        private readonly IBlobStore _blobStore;
        private readonly DeduplicationHelper _deduplicationHelper;
        private readonly ICollectionWrapper<DocumentDescriptorReadModel, DocumentDescriptorId> _documents;
        

        public DocumentWorkflow(
            ICommandBus commandBus, 
            IBlobStore blobStore, 
            DeduplicationHelper deduplicationHelper,
            ICollectionWrapper<DocumentDescriptorReadModel, DocumentDescriptorId> documents)
        {
            _commandBus = commandBus;
            _blobStore = blobStore;
            _deduplicationHelper = deduplicationHelper;
            _documents = documents;
        }

        public override void Drop()
        {

        }

        public override void SetUp()
        {
        }

        public void On(DocumentDescriptorCreated e)
        {
            if (IsReplay) return;
  
            //This projection depends on the projection of document descriptor
            //to execute the workflow.
            var descriptor = _documents.FindOneById((DocumentDescriptorId)e.AggregateId);

            _commandBus.Send(new LinkDocumentToDocumentDescriptor(
                (DocumentDescriptorId)e.AggregateId,
                e.HandleInfo)
                .WithDiagnosticTriggeredByInfo(e, "Queued for processing of " + e.AggregateId)
            );
        }

        public void On(DocumentDescriptorHasBeenDeduplicated e)
        {
            if (IsReplay)
                return;

            _commandBus.Send(new LinkDocumentToDocumentDescriptor(
                (DocumentDescriptorId)e.AggregateId,
                e.HandleInfo)
                .WithDiagnosticTriggeredByInfo(e, "Document " + e.OtherDocumentId + " deduplicated to " + e.AggregateId)
            );

            _commandBus.Send(new DeleteDocumentDescriptor(e.OtherDocumentId,DocumentHandle.Empty)
                .WithDiagnosticTriggeredByInfo(e,"deduplication of " + e.AggregateId)
            );
        }

        public void On(FormatAddedToDocumentDescriptor e)
        {
            if (IsReplay)
                return;

            var descriptor = _blobStore.GetDescriptor(e.BlobId);
            Logger.DebugFormat("Next conversion step for document {0} {1}", e.BlobId, descriptor.FileNameWithExtension);
        }

        public void On(DocumentDescriptorInitialized e)
        {
            if (IsReplay)
                return;

            var thisDocumentId = (DocumentDescriptorId)e.AggregateId;

            var duplicatedId = _deduplicationHelper.FindDuplicateDocumentId(
                thisDocumentId,
                e.Hash,
                e.BlobId
                );

            if (duplicatedId != null)
            {
                _commandBus.Send(new DeduplicateDocumentDescriptor(
                    duplicatedId, 
                    thisDocumentId, 
                    e.HandleInfo)
                    .WithDiagnosticTriggeredByInfo(e)                        
                );
            }
            else
            {
                _commandBus.Send(new CreateDocumentDescriptor(thisDocumentId, e.HandleInfo)
                    .WithDiagnosticTriggeredByInfo(e)                        
                );
            }

            if (e.FatherDocumentDescriptorId != null) 
            {
                //This handle is created as attachment of another document.
                _commandBus.Send(new CreateAttachment(
                    e.FatherDocumentDescriptorId, 
                    e.HandleInfo.Handle, 
                    e.HandleInfo.CustomData[JobsConstants.AttachmentRelativePath] as String)
                        .WithDiagnosticTriggeredByInfo(e)
                );
            }
        }

        public void On(DocumentDeleted e)
        {
            if (IsReplay) return;

            _commandBus.Send(new DeleteDocumentDescriptor(e.DocumentDescriptorId, e.Handle)
                .WithDiagnosticTriggeredByInfo(e, "Handle deleted")
            );
        }

        public void On(DocumentLinked e)
        {
            if (IsReplay) return;

            if (e.PreviousDocumentId != null)
                _commandBus.Send(new DeleteDocumentDescriptor(e.PreviousDocumentId, e.Handle)
                    .WithDiagnosticTriggeredByInfo(e,string.Format("Handle relinked from {0} to {1}", e.PreviousDocumentId, e.DocumentId))
                );
        }

        public void On(DocumentDescriptorDeleted e)
        {
            if (IsReplay) return;

            if (e.Attachments != null && e.Attachments.Length > 0) 
            {
                //delete all orphaned attachments
                foreach (var attachment in e.Attachments)
                {
                    _commandBus.Send(new DeleteDocument(attachment)
                        .WithDiagnosticTriggeredByInfo(e, string.Format("Delete orphaned attachment {0} belonging to dleted descriptor {1}", 
                            attachment, e.AggregateId))
                    );
                }
            }
             
        }
    }
}