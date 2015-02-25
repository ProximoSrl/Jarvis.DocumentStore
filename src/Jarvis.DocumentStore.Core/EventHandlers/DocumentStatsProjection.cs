using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.Framework.Kernel.Events;
using Jarvis.Framework.Kernel.ProjectionEngine;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class DocumentStatsProjection : AbstractProjection,
        IEventHandler<DocumentDescriptorCreated>,
        IEventHandler<DocumentDescriptorDeleted>
    {
        readonly ICollectionWrapper<DocumentStats, string> _collection;
        readonly IBlobStore _blobStore;

        public DocumentStatsProjection(ICollectionWrapper<DocumentStats, string> collection, IBlobStore blobStore)
        {
            _collection = collection;
            _blobStore = blobStore;
            _collection.Attach(this, false);
        }

        public override void Drop()
        {
            _collection.Drop();
        }

        public override void SetUp()
        {
        }

        public void On(DocumentDescriptorCreated e)
        {
            var descriptor = _blobStore.GetDescriptor(e.BlobId);
            if (descriptor != null)
            {
                _collection.Upsert(e, e.HandleInfo.FileName.Extension,
                    () => new DocumentStats()
                    {
                        Files = 1,
                        Bytes = descriptor.Length
                    },
                    s =>
                    {
                        s.Files++;
                        s.Bytes += descriptor.Length;
                    });
            }
        }

        public void On(DocumentDescriptorDeleted e)
        {
            var descriptor = _blobStore.GetDescriptor(e.BlobId);
            if (descriptor != null)
            {
                _collection.FindAndModify(e, descriptor.FileNameWithExtension.Extension,
                    s =>
                    {
                        s.Files--;
                        s.Bytes -= descriptor.Length;
                    });
            }
        }
    }
}
