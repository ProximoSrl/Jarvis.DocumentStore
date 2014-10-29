using System.Collections.Generic;
using CQRS.Kernel.Engine;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    public class DocumentState : AggregateState
    {
        public IDictionary<DocumentFormat, FileId> Formats { get; private set; }
        public HashSet<DocumentHandle>  Handles { get; private set; }
        public FileId FileId { get; private set; }
        
        public DocumentState(params KeyValuePair<DocumentFormat, FileId>[] formats)
            : this()
        {
            foreach (var keyValuePair in formats)
            {
                this.Formats.Add(keyValuePair);
            }
        }

        public DocumentState()
        {
            this.Formats = new Dictionary<DocumentFormat, FileId>();
            this.Handles = new HashSet<DocumentHandle>();
        }

        void When(DocumentDeleted e)
        {
            this.HasBeenDeleted = true;
        }

        public bool HasBeenDeleted { get; private set; }

        void When(DocumentCreated e)
        {
            this.AggregateId = e.AggregateId;
            this.FileId = e.FileId;
            this.Handles.Add(e.Handle);
        }

        void When(FormatAddedToDocument e)
        {
            this.Formats.Add(e.DocumentFormat, e.FileId);
        }

        private void When(DocumentHandleAttached e)
        {
            this.Handles.Add(e.Handle);
        }

        void When(DocumentHandleDetached e)
        {
            this.Handles.Remove(e.Handle);
        }

        void When(DocumentFormatHasBeenDeleted e)
        {
            this.Formats.Remove(e.DocumentFormat);
        }

        public bool HasFormat(DocumentFormat documentFormat)
        {
            return Formats.ContainsKey(documentFormat);
        }

        public bool IsValidHandle(DocumentHandle handle)
        {
            return Handles.Contains(handle);
        }
    }
}