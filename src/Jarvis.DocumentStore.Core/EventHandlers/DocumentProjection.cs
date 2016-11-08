using System.Linq;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.Framework.Kernel.Events;
using Jarvis.Framework.Kernel.ProjectionEngine;
using NEventStore;
using System;
using Jarvis.Framework.Shared.ReadModel;
using MongoDB.Driver;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class DocumentProjection : AbstractProjection
        , IEventHandler<DocumentInitialized>
        , IEventHandler<DocumentLinked>
        , IEventHandler<DocumentFileNameSet>
        , IEventHandler<DocumentCustomDataSet>
        , IEventHandler<DocumentDeleted>
        , IEventHandler<DocumentDescriptorHasBeenDeduplicated>
        , IEventHandler<DocumentDescriptorInitialized>
    {
        readonly IDocumentWriter _writer;
        private readonly IReader<DocumentDescriptorReadModel, DocumentDescriptorId> _documentDescriptorReader;
        private readonly ICollectionWrapper<DocumentDeletedReadModel, String> _documentDeletedWrapper;

        public DocumentProjection(
            IDocumentWriter writer,
            IReader<DocumentDescriptorReadModel, DocumentDescriptorId> documentDescriptorReader,
            ICollectionWrapper<DocumentDeletedReadModel, String> documentDeletedWrapper)
        {
            _writer = writer;
            _documentDescriptorReader = documentDescriptorReader;
            _documentDeletedWrapper = documentDeletedWrapper;
            _documentDeletedWrapper.Attach(this, false);
        }

        public override int Priority
        {
            get { return 10; }
        }

        public override void Drop()
        {
            _writer.Drop();
        }

        public override void SetUp()
        {
            _writer.Init();
            _documentDeletedWrapper.CreateIndex("Handle",
                Builders<DocumentDeletedReadModel>.IndexKeys.Ascending(d => d.Handle));
        }

        public void On(DocumentLinked e)
        {
            _writer.LinkDocument(
                e.Handle,
                e.DocumentId,
                e.CheckpointToken
            );
        }

        public void On(DocumentDescriptorInitialized e)
        {
            //need eager association with descriptor to correctly manage association
            //with attachment.
            _writer.CreateIfMissing(
                 e.HandleInfo.Handle,
                 e.Id,
                 e.CheckpointToken
             );
        }

        public void On(DocumentCustomDataSet e)
        {
            _writer.UpdateCustomData(e.Handle, e.CustomData);
        }

        public void On(DocumentInitialized e)
        {
            _writer.CreateIfMissing(
                e.Handle,
                null,
                e.CheckpointToken
            );
        }

        public void On(DocumentDeleted e)
        {
            _writer.Delete(e.Handle, e.CheckpointToken);
            _documentDeletedWrapper.Insert(e, new DocumentDeletedReadModel()
            {
                Id = e.Handle + "+" + e.DocumentDescriptorId + "+" + e.CheckpointToken,
                DeletionDate = e.CommitStamp,
                Handle = e.Handle,
                DocumentDescriptorId = e.DocumentDescriptorId
            },
            false);
        }

        public void On(DocumentFileNameSet e)
        {
            _writer.SetFileName(e.Handle, e.FileName, e.CheckpointToken);
        }

        public void On(DocumentDescriptorHasBeenDeduplicated e)
        {
            var originalDocumentDescriptor = _documentDescriptorReader.AllUnsorted.Single(d => d.Id == e.AggregateId); 
            _writer.DocumentDeDuplicated(
                e.HandleInfo.Handle,
                null,
                (DocumentDescriptorId)e.AggregateId,
                e.CheckpointToken
            );
        }
    }
}
