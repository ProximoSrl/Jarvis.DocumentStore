using System.Linq;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.Framework.Kernel.Events;
using Jarvis.Framework.Kernel.ProjectionEngine;
using NEventStore;

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
        private readonly ICollectionWrapper<DocumentDescriptorReadModel, DocumentDescriptorId> _documentDescriptorCollectionWrapper;

        public DocumentProjection(
            IDocumentWriter writer, 
            ICollectionWrapper<DocumentDescriptorReadModel, DocumentDescriptorId> documentDescriptorCollectionWrapper)
        {
            _writer = writer;
            _documentDescriptorCollectionWrapper = documentDescriptorCollectionWrapper;
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
        }

        public void On(DocumentLinked e)
        {
            _writer.LinkDocument(
                e.Handle,
                e.DocumentId,
                LongCheckpoint.Parse(e.CheckpointToken).LongValue
            );
        }

        public void On(DocumentDescriptorInitialized e)
        {
            //need eager association with descriptor to correctly manage association
            //with attachment.
            _writer.CreateIfMissing(
                 e.HandleInfo.Handle,
                 e.Id,
                 LongCheckpoint.Parse(e.CheckpointToken).LongValue
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
                LongCheckpoint.Parse(e.CheckpointToken).LongValue
            );
        }

        public void On(DocumentDeleted e)
        {
            _writer.Delete(e.Handle, LongCheckpoint.Parse(e.CheckpointToken).LongValue);
        }

        public void On(DocumentFileNameSet e)
        {
            _writer.SetFileName(e.Handle, e.FileName, LongCheckpoint.Parse(e.CheckpointToken).LongValue);
        }

        public void On(DocumentDescriptorHasBeenDeduplicated e)
        {
            var originalDocumentDescriptor = _documentDescriptorCollectionWrapper.All.Single(d => d.Id == e.AggregateId); 
            _writer.DocumentDeDuplicated(
                e.Handle,
                null,
                (DocumentDescriptorId)e.AggregateId,
                LongCheckpoint.Parse(e.CheckpointToken).LongValue
            );
        }
    }
}
