using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.Framework.Kernel.Events;
using NEventStore;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class DocumentProjection : AbstractProjection
        ,IEventHandler<DocumentInitialized>
        ,IEventHandler<DocumentLinked>
        ,IEventHandler<DocumentFileNameSet>
        ,IEventHandler<DocumentCustomDataSet>
        ,IEventHandler<DocumentDeleted>
        ,IEventHandler<DocumentDescriptorHasBeenDeduplicated>
    {
        readonly IHandleWriter _writer;

        public DocumentProjection(IHandleWriter writer)
        {
            _writer = writer;
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

        public void On(DocumentCustomDataSet e)
        {
            _writer.UpdateCustomData(e.Handle, e.CustomData);
        }

        public void On(DocumentInitialized e)
        {
            _writer.CreateIfMissing(
                e.Handle,
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
            _writer.LinkDocument(
                e.Handle,
                (DocumentDescriptorId)e.AggregateId,
                LongCheckpoint.Parse(e.CheckpointToken).LongValue
            );
        }

        /// <summary>
        /// Need to maintain the chain of the attachment.
        /// </summary>
        /// <param name="e"></param>
        public void On(DocumentHasNewAttachment e)
        {
            _writer.AddAttachment(
                e.Handle,
                e.Attachment);
        }
    }
}
