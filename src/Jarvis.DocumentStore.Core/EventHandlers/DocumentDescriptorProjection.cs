using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.Framework.Kernel.Events;
using Jarvis.Framework.Kernel.ProjectionEngine;
using MongoDB.Driver.Builders;
using System.Collections.Generic;
using System.Linq;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class DocumentDescriptorProjection : AbstractProjection,
        IEventHandler<DocumentDescriptorInitialized>,
        IEventHandler<FormatAddedToDocumentDescriptor>,
        IEventHandler<DocumentDescriptorDeleted>,
        IEventHandler<DocumentHandleAttached>,
        IEventHandler<DocumentHandleDetached>,
        IEventHandler<DocumentFormatHasBeenUpdated>,
        IEventHandler<DocumentDescriptorHasNewAttachment>
    {
        private readonly ICollectionWrapper<DocumentDescriptorReadModel, DocumentDescriptorId> _documents;
        private IDocumentWriter _handleWriter;
        public DocumentDescriptorProjection(
            ICollectionWrapper<DocumentDescriptorReadModel, DocumentDescriptorId> documents, IDocumentWriter handleWriter)
        {
            _documents = documents;
            _handleWriter = handleWriter;

            _documents.Attach(this, false);

            _documents.OnSave = (d, e) =>
            {
                d.FormatsCount = d.Formats.Count;
            };
        }

        public override int Priority
        {
            get { return 5; }
        }

        public override void Drop()
        {
            _documents.Drop();
        }

        public override void SetUp()
        {
            _documents.CreateIndex(IndexKeys<DocumentDescriptorReadModel>.Ascending(x => x.Hash));
        }

        public void On(DocumentDescriptorInitialized e)
        {
            var document = new DocumentDescriptorReadModel((DocumentDescriptorId)e.AggregateId, e.BlobId)
            {
                Hash = e.Hash
            };

            _documents.Insert(e, document);
        }

        public void On(FormatAddedToDocumentDescriptor e)
        {
            _documents.FindAndModify(e, (DocumentDescriptorId)e.AggregateId, d =>
            {
                d.AddFormat(e.CreatedBy, e.DocumentFormat, e.BlobId);
            });
        }

        public void On(DocumentDescriptorDeleted e)
        {
            _documents.Delete(e, (DocumentDescriptorId)e.AggregateId);
        }

        public void On(DocumentHandleAttached e)
        {
            _documents.FindAndModify(e, (DocumentDescriptorId)e.AggregateId, d => d.AddHandle(e.Handle));
        }

        public void On(DocumentHandleDetached e)
        {
            _documents.FindAndModify(e, (DocumentDescriptorId)e.AggregateId, d => d.Remove(e.Handle));
        }

        public void On(DocumentFormatHasBeenUpdated e)
        {
            _documents.FindAndModify(e, (DocumentDescriptorId)e.AggregateId, d =>
            {
                d.AddFormat(e.CreatedBy, e.DocumentFormat, e.BlobId);
            });
        }

        /// <summary>
        /// Need to maintain the chain of the attachment.
        /// </summary>
        /// <param name="e"></param>
        public void On(DocumentDescriptorHasNewAttachment e)
        {
            _documents.FindAndModify(e, (DocumentDescriptorId) e.AggregateId, d =>
            {
                d.AddAttachments(e.Attachment, e.AttachmentPath);
            });
        }
    }
}
