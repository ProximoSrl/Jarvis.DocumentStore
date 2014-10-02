using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Kernel.Events;
using CQRS.Kernel.ProjectionEngine;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class DocumentMapperHandler : AbstractProjection
        ,IEventHandler<DocumentCreated>
    {
        readonly ICollectionWrapper<DocumentMapping, FileAlias> _mapping;

        public DocumentMapperHandler(ICollectionWrapper<DocumentMapping, FileAlias> mapping)
        {
            _mapping = mapping;
            _mapping.Attach(this, false);
        }

        public override void Drop()
        {
            _mapping.Drop();
        }

        public override void SetUp()
        {
            
        }

        public void On(DocumentCreated e)
        {
            _mapping.Upsert(e, e.Alias, () => new DocumentMapping(){
                DocumentId = (DocumentId) e.AggregateId
            }, 
                m => m.DocumentId = (DocumentId)e.AggregateId
            );
        }
    }
}
