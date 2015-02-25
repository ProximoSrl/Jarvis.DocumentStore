using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.Handle.Events;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.Framework.Kernel.Events;
using Jarvis.Framework.Kernel.ProjectionEngine;
using Jarvis.Framework.Shared.ReadModel;
using MongoDB.Bson;
using NEventStore;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class StreamProjection : AbstractProjection,
         IEventHandler<HandleInitialized>,
         IEventHandler<HandleDeleted>,
         IEventHandler<HandleLinked>,
         IEventHandler<FormatAddedToDocumentDescriptor>,
         IEventHandler<HandleFileNameSet>,
         IEventHandler<DocumentFormatHasBeenUpdated>
    {
        private readonly ICollectionWrapper<StreamReadModel, Int64> _streamReadModelCollection;
        private readonly IReader<DocumentDescriptorReadModel, DocumentDescriptorId> _documentReadModel;
        private readonly IHandleWriter _handleWriter;
        private readonly IBlobStore _blobStore;

        private Int64 _lastCheckpointValue = -1;
        private Int32 _sequential = 0;
        private Int64 _lastId;

        public StreamProjection(
            ICollectionWrapper<StreamReadModel, Int64> streamReadModelCollection,
            IHandleWriter handleWriter,
            IBlobStore blobStore,
            IReader<DocumentDescriptorReadModel, DocumentDescriptorId> documentReadModel)
        {
            _streamReadModelCollection = streamReadModelCollection;
            _documentReadModel = documentReadModel;
            _handleWriter = handleWriter;
            _blobStore = blobStore;
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
                //TenantId = this.TenantId,
                Handle = e.Handle,
                EventType = HandleStreamEventTypes.HandleInitialized,
            });
        }

        public void On(HandleFileNameSet e)
        {
            var handle = _handleWriter.FindOneById(e.Handle);
            _streamReadModelCollection.Insert(e, new StreamReadModel()
            {
                Id = GetNewId(),
                //TenantId = this.TenantId,
                Handle = e.Handle,
                HandleCustomData = handle.CustomData,
                EventType = HandleStreamEventTypes.HandleFileNameSet
            });
        }

        public void On(HandleDeleted e)
        {
            _streamReadModelCollection.Insert(e, new StreamReadModel()
            {
                Id = GetNewId(),
                //TenantId = this.TenantId,
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
                var descriptor = _blobStore.GetDescriptor(format.Value.BlobId);
                _streamReadModelCollection.Insert(e, new StreamReadModel()
                {
                    Id = GetNewId(),
                    //TenantId = this.TenantId,
                    Handle = e.Handle,
                    Filename = descriptor.FileNameWithExtension,
                    DocumentId = e.DocumentId,
                    FormatInfo = new FormatInfo()
                    {
                        BlobId = format.Value.BlobId,
                        DocumentFormat = format.Key,
                        PipelineId = format.Value.PipelineId != PipelineId.Null 
                            ? format.Value.PipelineId 
                            : new PipelineId("original"),
                    },
                    EventType = HandleStreamEventTypes.HandleHasNewFormat,
                    HandleCustomData = handle.CustomData,
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void On(FormatAddedToDocumentDescriptor e)
        {
            var allHandles = _documentReadModel.FindOneById((DocumentDescriptorId) e.AggregateId).Handles;
            var descriptor = _blobStore.GetDescriptor(e.BlobId);
            foreach (var handle in allHandles)
            {
                var handleReadMode = _handleWriter.FindOneById(handle);
                _streamReadModelCollection.Insert(e, new StreamReadModel()
                {
                    Id = GetNewId(),
                    //TenantId = this.TenantId,
                    Handle = handle,
                    Filename = descriptor.FileNameWithExtension,
                    DocumentId = (DocumentDescriptorId) e.AggregateId,
                    FormatInfo = new FormatInfo()
                    {
                        BlobId = e.BlobId,
                        DocumentFormat = e.DocumentFormat,
                        PipelineId = e.CreatedBy != PipelineId.Null
                            ? e.CreatedBy
                            : new PipelineId("original"),
                    },
                    EventType = HandleStreamEventTypes.HandleHasNewFormat,
                    HandleCustomData = handleReadMode.CustomData,
                });
            }
        }



        public void On(DocumentFormatHasBeenUpdated e)
        {
            var allHandles = _documentReadModel.FindOneById((DocumentDescriptorId)e.AggregateId).Handles;
            var descriptor = _blobStore.GetDescriptor(e.BlobId);
            foreach (var handle in allHandles)
            {
                var handleReadMode = _handleWriter.FindOneById(handle);
                _streamReadModelCollection.Insert(e, new StreamReadModel()
                {
                    Id = GetNewId(),
                    //TenantId = this.TenantId,
                    Handle = handle,
                    Filename = descriptor.FileNameWithExtension,
                    DocumentId = (DocumentDescriptorId)e.AggregateId,
                    FormatInfo = new FormatInfo()
                    {
                        BlobId = e.BlobId,
                        DocumentFormat = e.DocumentFormat,
                        PipelineId = e.CreatedBy != PipelineId.Null
                            ? e.CreatedBy
                            : new PipelineId("original"),
                    },
                    EventType = HandleStreamEventTypes.HandleFormatUpdated,
                    HandleCustomData = handleReadMode.CustomData,
                });
            }
        }
    }
}
