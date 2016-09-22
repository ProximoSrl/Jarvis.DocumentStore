using System.Collections.Generic;
using System.Linq;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Kernel.Engine;
using System;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor
{

    public class DocumentDescriptorState : AggregateState
    {
        private HashSet<DocumentHandle> _handles = new HashSet<DocumentHandle>();

        public DocumentDescriptorState(params KeyValuePair<DocumentFormat, BlobId>[] formats)
            : this()
        {
            foreach (var keyValuePair in formats)
            {
                Formats.Add(keyValuePair);
            }
        }

        public DocumentDescriptorState()
        {
            Formats = new Dictionary<DocumentFormat, BlobId>();
            Attachments = new List<DocumentHandle>();
        }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public IDictionary<DocumentFormat, BlobId> Formats { get; private set; }
        public BlobId BlobId { get; private set; }

        public bool HasBeenDeleted { get; private set; }
        public bool Created { get; private set; }

        public IList<DocumentHandle> Attachments { get; private set; }

        public DocumentHandleInfo CreationDocumentHandleInfo { get; set; }

        protected override object DeepCloneMe()
        {
            DocumentDescriptorState cloned = new DocumentDescriptorState();
            cloned.HasBeenDeleted = HasBeenDeleted;
            cloned.Created = Created;
            cloned.BlobId = BlobId;
  
            cloned._handles = new HashSet<DocumentHandle>();
            if (_handles != null)
            {
                foreach (var handle in _handles)
                {
                    cloned._handles.Add(handle);
                }
            }
            foreach (var format in Formats)
            {
                cloned.Formats.Add(format.Key, format.Value);
            }
            cloned.Attachments = Attachments.ToList();

            if (CreationDocumentHandleInfo != null)
                cloned.CreationDocumentHandleInfo = CreationDocumentHandleInfo.Clone();
            return cloned;    
        }

        public void AddAttachment(DocumentHandle attachment)
        {
            Attachments.Add(attachment);
        }

        public void RemoveAttachment(DocumentHandle attachment)
        {
            if (Attachments.Contains(attachment)) Attachments.Remove(attachment);
        }

        private void When(DocumentDescriptorDeleted e)
        {
            HasBeenDeleted = true;
        }

        private void When(DocumentDescriptorInitialized e)
        {
            BlobId = e.BlobId;
        }

        private void When(DocumentDescriptorCreated e)
        {
            CreationDocumentHandleInfo = e.HandleInfo;
            Created = true;
        }

        private void When(FormatAddedToDocumentDescriptor e)
        {
            Formats.Add(e.DocumentFormat, e.BlobId);
        }

        private void When(DocumentFormatHasBeenDeleted e)
        {
            Formats.Remove(e.DocumentFormat);
        }

        private void When(DocumentHandleAttached e)
        {
            _handles.Add(e.Handle);
        }

        private void When(DocumentHandleDetached e)
        {
            _handles.Remove(e.Handle);
        }

        public bool HasFormat(DocumentFormat documentFormat)
        {
            return Formats.ContainsKey(documentFormat);
        }

        public bool IsValidHandle(DocumentHandle handle)
        {
            return _handles.Contains(handle);
        }

        public bool HasActiveHandles()
        {
            return _handles.Any();
        }

        private void When(DocumentDescriptorHasNewAttachment e)
        {
            AddAttachment(e.Attachment);
        }
    }
}