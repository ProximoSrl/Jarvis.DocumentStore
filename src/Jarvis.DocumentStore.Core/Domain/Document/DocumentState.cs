using System.Collections.Generic;
using System.Linq;
using CQRS.Kernel.Engine;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    public class DocumentState : AggregateState
    {
        private readonly HashSet<DocumentHandle> _handles = new Quartz.Collection.HashSet<DocumentHandle>();

        public DocumentState(params KeyValuePair<DocumentFormat, BlobId>[] formats)
            : this()
        {
            foreach (var keyValuePair in formats)
            {
                Formats.Add(keyValuePair);
            }
        }

        public DocumentState()
        {
            Formats = new Dictionary<DocumentFormat, BlobId>();
        }

        public IDictionary<DocumentFormat, BlobId> Formats { get; private set; }
        public BlobId BlobId { get; private set; }

        public bool HasBeenDeleted { get; private set; }

        private void When(DocumentDeleted e)
        {
            HasBeenDeleted = true;
        }

        private void When(DocumentCreated e)
        {
            AggregateId = e.AggregateId;
            BlobId = e.BlobId;
        }

        private void When(FormatAddedToDocument e)
        {
            Formats.Add(e.DocumentFormat, e.BlobId);
        }

        private void When(DocumentFormatHasBeenDeleted e)
        {
            Formats.Remove(e.DocumentFormat);
        }

        private void When(DocumentHandleAttacched e)
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
    }
}