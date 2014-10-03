using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Kernel.Events;
using CQRS.Kernel.ProjectionEngine.RecycleBin;
using Jarvis.DocumentStore.Core.Domain.Document.Events;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class RecycleBinProjection : AbstractProjection
        ,IEventHandler<DocumentDeleted>
    {
        private readonly IRecycleBin _recycleBin;

        public RecycleBinProjection(IRecycleBin recycleBin)
        {
            _recycleBin = recycleBin;
        }

        public override void Drop()
        {
            _recycleBin.Drop();
        }

        public override void SetUp()
        {
        }

        public void On(DocumentDeleted e)
        {
            var files = e.FileFormatsId.Concat(new []{ e.FileId}).ToArray();
            _recycleBin.Delete("Jarvis", e.AggregateId, new { files });
        }
    }
}
