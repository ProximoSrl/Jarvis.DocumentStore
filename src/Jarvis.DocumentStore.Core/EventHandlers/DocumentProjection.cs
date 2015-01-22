using System;
using System.Text;
using System.Threading.Tasks;
using CQRS.Kernel.Events;
using CQRS.Kernel.ProjectionEngine;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.ReadModel;
using MongoDB.Driver.Builders;
using NEventStore;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class DocumentProjection : AbstractProjection,
        IEventHandler<DocumentCreated>,
        IEventHandler<FormatAddedToDocument>,
        IEventHandler<DocumentDeleted>,
        IEventHandler<DocumentHandleAttached>,
        IEventHandler<DocumentHandleDetached>,
        IEventHandler<DocumentFormatHasBeenUpdated>
    {
        private readonly ICollectionWrapper<DocumentReadModel, DocumentId> _documents;
        private IHandleWriter _handleWriter;
        public DocumentProjection(
            ICollectionWrapper<DocumentReadModel, DocumentId> documents, IHandleWriter handleWriter)
        {
            _documents = documents;
            _handleWriter = handleWriter;

            _documents.Attach(this, false);

            _documents.OnSave = d =>
            {
                d.FormatsCount = d.Formats.Count;
            };
        }

        public override int Priority
        {
            get { return 10; }
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

        public void On(DocumentHandleAttached e)
        {
            _documents.FindAndModify(e, (DocumentId)e.AggregateId, d => d.AddHandle(e.Handle));
        }

        public void On(DocumentHandleDetached e)
        {
            _documents.FindAndModify(e, (DocumentId)e.AggregateId, d => d.Remove(e.Handle));
        }

        public void On(DocumentFormatHasBeenUpdated e)
        {
            _documents.FindAndModify(e, (DocumentId)e.AggregateId, d =>
            {
                d.AddFormat(e.CreatedBy, e.DocumentFormat, e.BlobId);
            });
        }
    }
}
