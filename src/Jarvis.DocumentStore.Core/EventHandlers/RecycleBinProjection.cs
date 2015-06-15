using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.Framework.Kernel.Events;
using Jarvis.Framework.Kernel.ProjectionEngine.RecycleBin;
using Jarvis.DocumentStore.Core.Domain.Document.Events;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class RecycleBinProjection : AbstractProjection
        ,IEventHandler<DocumentDescriptorDeleted>
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

        public void On(DocumentDescriptorDeleted e)
        {
            var files = e.BlobFormatsId.Concat(new []{ e.BlobId}).ToArray();
            _recycleBin.Delete(e.AggregateId, "Jarvis", e.CommitStamp, new { files });
        }

        /// <summary>
        /// Delete handle put the handle in the recycle bin
        /// </summary>
        /// <param name="e"></param>
        public void On(DocumentDeleted e)
        {
            _recycleBin.Delete(e.AggregateId, "Jarvis", e.CommitStamp);
        }
    }
}
