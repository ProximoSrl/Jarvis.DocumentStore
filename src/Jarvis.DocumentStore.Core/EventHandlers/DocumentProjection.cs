using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Kernel.Events;
using CQRS.Kernel.ProjectionEngine;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.ReadModel;
using MongoDB.Driver.Builders;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class DocumentProjection : AbstractProjection,
        IEventHandler<DocumentCreated>,
        IEventHandler<FormatAddedToDocument>,
        IEventHandler<DocumentDeleted>,
        IEventHandler<DocumentHasBeenDeduplicated>,
        IEventHandler<DocumentHandleDetached>
    {
        private readonly ICollectionWrapper<DocumentReadModel, DocumentId> _documents;

        public DocumentProjection(ICollectionWrapper<DocumentReadModel, DocumentId> documents)
        {
            _documents = documents;
            _documents.Attach(this,false);

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
            _documents.CreateIndex(IndexKeys<DocumentReadModel>.Ascending(x=>x.MappedHandles));
        }

        public void On(DocumentCreated e)
        {
            _documents.Insert(e, new DocumentReadModel(
                (DocumentId)e.AggregateId,
                e.FileId,
                e.Handle,
                e.FileName
            ));
        }

        public void On(FormatAddedToDocument e)
        {
            _documents.FindAndModify(e, (DocumentId)e.AggregateId, d => {
                d.AddFormat(e.CreatedBy, e.DocumentFormat, e.FileId);
            });
        }

        public void On(DocumentDeleted e)
        {
            _documents.Delete(e, (DocumentId) e.AggregateId);
        }

        public void On(DocumentHasBeenDeduplicated e)
        {
            _documents.FindAndModify(e, (DocumentId)e.AggregateId, d =>
            {
                d.AddHandle(e.OtherDocumentHandle, e.OtherFileName);
            });
        }

        public void On(DocumentHandleDetached e)
        {
            _documents.FindAndModify(e, (DocumentId)e.AggregateId, d =>
            {
                d.RemoveHandle(e.Handle);
            });
        }
    }
}
