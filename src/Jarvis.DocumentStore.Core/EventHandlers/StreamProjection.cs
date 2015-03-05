using System;
using System.Collections.Generic;
using System.Linq;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.Framework.Kernel.Events;
using Jarvis.Framework.Kernel.ProjectionEngine;
using Jarvis.Framework.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Shared.Model;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class StreamProjection : AbstractProjection,
         IEventHandler<DocumentInitialized>,
         IEventHandler<DocumentDeleted>,
         IEventHandler<DocumentLinked>,
         IEventHandler<FormatAddedToDocumentDescriptor>,
         IEventHandler<DocumentFileNameSet>,
         IEventHandler<DocumentFormatHasBeenUpdated>,
         IEventHandler<DocumentHasNewAttachment>
    {
        private readonly ICollectionWrapper<StreamReadModel, Int64> _streamReadModelCollection;
        private readonly IReader<DocumentDescriptorReadModel, DocumentDescriptorId> _documentDescriptorReadModel;
        private readonly IDocumentWriter _documentWriter;
        private readonly IBlobStore _blobStore;
        
        private Int64 _lastCheckpointValue = -1;
        private Int32 _sequential = 0;
        private Int64 _lastId;

        public StreamProjection(
            ICollectionWrapper<StreamReadModel, Int64> streamReadModelCollection,
            IDocumentWriter documentWriter,
            IBlobStore blobStore,
            IReader<DocumentDescriptorReadModel, DocumentDescriptorId> documentDescriptorReadModel)
        {
            _streamReadModelCollection = streamReadModelCollection;
            _documentDescriptorReadModel = documentDescriptorReadModel;
            _documentWriter = documentWriter;
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

        public void On(DocumentInitialized e)
        {
            _streamReadModelCollection.Insert(e, new StreamReadModel()
            {
                Id = GetNewId(),
                //TenantId = this.TenantId,
                Handle = e.Handle,
                EventType = HandleStreamEventTypes.DocumentInitialized,
            });
        }

        public void On(DocumentFileNameSet e)
        {
            var handle = _documentWriter.FindOneById(e.Handle);
            _streamReadModelCollection.Insert(e, new StreamReadModel()
            {
                Id = GetNewId(),
                //TenantId = this.TenantId,
                Handle = e.Handle,
                DocumentCustomData = handle.CustomData,
                EventType = HandleStreamEventTypes.DocumentFileNameSet
            });
        }

        public void On(DocumentDeleted e)
        {
            _streamReadModelCollection.Insert(e, new StreamReadModel()
            {
                Id = GetNewId(),
                //TenantId = this.TenantId,
                Handle = e.Handle,
                EventType = HandleStreamEventTypes.DocumentDeleted
            });
        }

        public void On(DocumentLinked e)
        {
            var doc = _documentDescriptorReadModel.FindOneById(e.DocumentId);
            var handle = _documentWriter.FindOneById(e.Handle);
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
                    EventType = HandleStreamEventTypes.DocumentHasNewFormat,
                    DocumentCustomData = handle.CustomData,
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void On(FormatAddedToDocumentDescriptor e)
        {
            var allHandles = _documentDescriptorReadModel.FindOneById((DocumentDescriptorId)e.AggregateId).Documents;
            var descriptor = _blobStore.GetDescriptor(e.BlobId);
            foreach (var handle in allHandles)
            {
                var handleReadMode = _documentWriter.FindOneById(handle);
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
                    EventType = HandleStreamEventTypes.DocumentHasNewFormat,
                    DocumentCustomData = handleReadMode.CustomData,
                });
            }
        }



        public void On(DocumentFormatHasBeenUpdated e)
        {
            var allHandles = _documentDescriptorReadModel.FindOneById((DocumentDescriptorId)e.AggregateId).Documents;
            var descriptor = _blobStore.GetDescriptor(e.BlobId);
            foreach (var handle in allHandles)
            {
                var handleReadMode = _documentWriter.FindOneById(handle);
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
                    EventType = HandleStreamEventTypes.DocumentFormatUpdated,
                    DocumentCustomData = handleReadMode.CustomData,
                });
            }
        }

        public void On(DocumentHasNewAttachment e)
        {
            var attachmentDescriptor = _documentDescriptorReadModel.AllUnsorted.SingleOrDefault(d => d.Documents.Contains(e.Attachment));
            if (attachmentDescriptor == null) return;

            var streamReadModel = new StreamReadModel()
            {
                Id = GetNewId(),
                //TenantId = this.TenantId,
                Handle = e.Attachment,
                DocumentId = attachmentDescriptor.Id,
               
                EventType = HandleStreamEventTypes.DocumentHasNewAttachment,
            };
            var handle = _documentWriter.FindOneById(e.Attachment);
            streamReadModel.AddEventData(StreamReadModelEventDataKeys.FatherHandle, e.Handle);
            //var rootAttachment = handle.AttachmentPath.Split('/').FirstOrDefault();
            //streamReadModel.AddEventData(StreamReadModelEventDataKeys.RootAttachmentHandle, rootAttachment);
            _streamReadModelCollection.Insert(e, streamReadModel);

        }
    }
}
