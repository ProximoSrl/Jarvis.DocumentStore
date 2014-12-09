using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Kernel.Events;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.Handle.Events;
using Jarvis.DocumentStore.Core.ReadModel;
using NEventStore;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class HandleProjection : AbstractProjection
        ,IEventHandler<HandleInitialized>
        ,IEventHandler<HandleLinked>
        ,IEventHandler<HandleFileNameSet>
        ,IEventHandler<HandleCustomDataSet>
        ,IEventHandler<HandleDeleted>
        ,IEventHandler<DocumentHasBeenDeduplicated>
    {
        readonly IHandleWriter _writer;

        public HandleProjection(IHandleWriter writer)
        {
            _writer = writer;
        }

        public override void Drop()
        {
            _writer.Drop();
        }

        public override void SetUp()
        {
            _writer.Init();
        }

        public void On(HandleLinked e)
        {
            _writer.LinkDocument(
                e.Handle, 
                e.DocumentId, 
                LongCheckpoint.Parse(e.CheckpointToken).LongValue
            );
        }

        public void On(HandleCustomDataSet e)
        {
            _writer.UpdateCustomData(e.Handle, e.CustomData);
        }

        public void On(HandleInitialized e)
        {
            _writer.CreateIfMissing(
                e.Handle,
                LongCheckpoint.Parse(e.CheckpointToken).LongValue
            );
        }

        public void On(HandleDeleted e)
        {
            _writer.Delete(e.Handle, LongCheckpoint.Parse(e.CheckpointToken).LongValue);
        }

        public void On(HandleFileNameSet e)
        {
            _writer.SetFileName(e.Handle, e.FileName, LongCheckpoint.Parse(e.CheckpointToken).LongValue);
        }

        public void On(DocumentHasBeenDeduplicated e)
        {
            _writer.LinkDocument(
                e.Handle,
                (DocumentId)e.AggregateId,
                LongCheckpoint.Parse(e.CheckpointToken).LongValue
            );
        }
    }
}
