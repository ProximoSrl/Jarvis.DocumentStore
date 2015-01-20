using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.sun.tools.javac.comp;
using CQRS.Kernel.Events;
using CQRS.Kernel.ProjectionEngine;
using CQRS.Shared.Events;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.Handle.Events;
using Jarvis.DocumentStore.Core.ReadModel;
using MongoDB.Bson;
using NEventStore;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class StreamProjection : AbstractProjection,
         IEventHandler<HandleInitialized>,
         IEventHandler<HandleDeleted>,
         IEventHandler<HandleLinked>,
         IEventHandler<FormatAddedToDocument>,
         IEventHandler<HandleFileNameSet>
    {
        private readonly ICollectionWrapper<StreamReadModel, Int64> _streamReadModelCollection;
        private readonly IReader<DocumentReadModel, DocumentId> _documentReadModel;
        private readonly IHandleWriter _handleWriter;

        private Int64 _lastCheckpointValue = -1;
        private Int32 _sequential = 0;
        private Int64 _lastId;

        public StreamProjection(
            ICollectionWrapper<StreamReadModel, Int64> streamReadModelCollection,
            IHandleWriter handleWriter,
            IReader<DocumentReadModel, DocumentId> documentReadModel)
        {
            _streamReadModelCollection = streamReadModelCollection;
            _documentReadModel = documentReadModel;
            _handleWriter = handleWriter;
            _streamReadModelCollection.Attach(this, false);
            if (_streamReadModelCollection.All.Any())
            {
                _lastId = _streamReadModelCollection.All.Max(s => s.Id);
            }
            else
            {
                _lastId = 0;
            }
        }

        public override int Priority
        {
            get { return 5; }
        }

        private Int64 GetNewId()
        {
            return ++_lastId;
        }

        public override void Drop()
        {
            _streamReadModelCollection.Drop();
        }

        public override void SetUp()
        {

        }

        public void On(HandleInitialized e)
        {
            _streamReadModelCollection.Insert(e, new StreamReadModel()
            {
                Id = GetNewId(),
                TenantId = this.TenantId,
                Handle = e.Handle,
                EventType = HandleStreamEventTypes.HandleInitialized
            });
        }

        public void On(HandleFileNameSet e)
        {
            _streamReadModelCollection.Insert(e, new StreamReadModel()
            {
                Id = GetNewId(),
                TenantId = this.TenantId,
                Handle = e.Handle,
                EventType = HandleStreamEventTypes.HandleFileNameSet
            });
        }

        public void On(HandleDeleted e)
        {
            _streamReadModelCollection.Insert(e, new StreamReadModel()
            {
                Id = GetNewId(),
                TenantId = this.TenantId,
                Handle = e.Handle,
                EventType = HandleStreamEventTypes.HandleDeleted
            });
        }

        public void On(HandleLinked e)
        {
            var doc = _documentReadModel.FindOneById(e.DocumentId);
            var handle = _handleWriter.FindOneById(e.Handle);
            foreach (var format in doc.Formats)   
            {
                _streamReadModelCollection.Insert(e, new StreamReadModel()
                {
                    Id = GetNewId(),
                    TenantId = this.TenantId,
                    Handle = e.Handle,
                    Filename = handle.FileName,
                    FormatInfo = new FormatInfo()
                    {
                        BlobId = format.Value.BlobId,
                        DocumentFormat = format.Key,
                        PipelineId = format.Value.PipelineId,
                    },
                    EventType = HandleStreamEventTypes.HandleHasNewFormat
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void On(FormatAddedToDocument e)
        {
            var allHandles = _documentReadModel.FindOneById((DocumentId) e.AggregateId).Handles;
      
            foreach (var handle in allHandles)
            {
                var handlerm = _handleWriter.FindOneById(handle);
                _streamReadModelCollection.Insert(e, new StreamReadModel()
                {
                    Id = GetNewId(),
                    TenantId = this.TenantId,
                    Handle = handle,
                    Filename = handlerm.FileName,
                    FormatInfo = new FormatInfo()
                    {
                        BlobId = e.BlobId,
                        DocumentFormat = e.DocumentFormat,
                        PipelineId = e.CreatedBy,
                    },
                    EventType = HandleStreamEventTypes.HandleHasNewFormat
                });
            }
        }

      
    }
}
