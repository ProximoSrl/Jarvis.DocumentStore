using System;
using System.Collections.Generic;
using System.Linq;
using CQRS.Kernel.Engine;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    public class DocumentState : AggregateState
    {
        public IDictionary<DocumentFormat, BlobId> Formats { get; private set; }
        public BlobId BlobId { get; private set; }
        
        public DocumentState(params KeyValuePair<DocumentFormat, BlobId>[] formats)
            : this()
        {
            foreach (var keyValuePair in formats)
            {
                this.Formats.Add(keyValuePair);
            }
        }

        public DocumentState()
        {
            this.Formats = new Dictionary<DocumentFormat, BlobId>();
        }

        void When(DocumentDeleted e)
        {
            this.HasBeenDeleted = true;
        }

        public bool HasBeenDeleted { get; private set; }

        void When(DocumentCreated e)
        {
            this.AggregateId = e.AggregateId;
            this.BlobId = e.BlobId;
        }

        void When(FormatAddedToDocument e)
        {
            this.Formats.Add(e.DocumentFormat, e.BlobId);
        }

        void When(DocumentFormatHasBeenDeleted e)
        {
            this.Formats.Remove(e.DocumentFormat);
        }

        public bool HasFormat(DocumentFormat documentFormat)
        {
            return Formats.ContainsKey(documentFormat);
        }
    }
}