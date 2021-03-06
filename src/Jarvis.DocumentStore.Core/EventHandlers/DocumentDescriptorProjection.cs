﻿using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.Framework.Kernel.Events;
using Jarvis.Framework.Kernel.ProjectionEngine;
using MongoDB.Driver;

using NEventStore;
using System.Collections.Generic;
using System.Linq;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class DocumentDescriptorProjection : AbstractProjection,
        IEventHandler<DocumentDescriptorInitialized>,
        IEventHandler<DocumentDescriptorCreated>,
        IEventHandler<FormatAddedToDocumentDescriptor>,
        IEventHandler<DocumentDescriptorDeleted>,
        IEventHandler<DocumentHandleAttached>,
        IEventHandler<DocumentHandleDetached>,
        IEventHandler<DocumentFormatHasBeenUpdated>,
        IEventHandler<DocumentDescriptorHasNewAttachment>,
        IEventHandler<DocumentFormatHasBeenDeleted>
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
            _documents.CreateIndex("Hash", Builders<DocumentDescriptorReadModel>.IndexKeys.Ascending(x => x.Hash));
            _documents.CreateIndex("Documents", Builders<DocumentDescriptorReadModel>.IndexKeys.Ascending(x => x.Documents));
        }

        public void On(DocumentDescriptorInitialized e)
        {
            var longCheckpoint = e.CheckpointToken;
            var document = new DocumentDescriptorReadModel(longCheckpoint, (DocumentDescriptorId)e.AggregateId, e.BlobId)
            {
                Hash = e.Hash
            };

            _documents.Insert(e, document);
        }

        /// <summary>
        /// Need to maintain the chain of the attachment.
        /// </summary>
        /// <param name="e"></param>
        public void On(DocumentDescriptorCreated e)
        {
            _documents.FindAndModify(e, (DocumentDescriptorId)e.AggregateId, d =>
            {
                d.Created = true;
            });
        }

        public void On(FormatAddedToDocumentDescriptor e)
        {
            _documents.FindAndModify(e, (DocumentDescriptorId)e.AggregateId, d =>
            {
                d.AddFormat(e.CreatedBy, e.DocumentFormat, e.BlobId);
            });
        }

        public void On(DocumentFormatHasBeenDeleted e)
        {
            _documents.FindAndModify(e, (DocumentDescriptorId)e.AggregateId, d =>
            {
                d.RemoveFormat(e.DocumentFormat);
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
