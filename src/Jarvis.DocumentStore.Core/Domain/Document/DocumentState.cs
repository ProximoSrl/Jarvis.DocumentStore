using System.Collections.Generic;
using CQRS.Kernel.Engine;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    public class DocumentState : AggregateState
    {
        public IDictionary<FormatValue, FileId> Formats { get; private set; }

        public DocumentState(params KeyValuePair<FormatValue, FileId>[] formats)
            : this()
        {
            foreach (var keyValuePair in formats)
            {
                this.Formats.Add(keyValuePair);
            }
        }

        public DocumentState()
        {
            this.Formats = new Dictionary<FormatValue, FileId>();
        }

        void When(DocumentCreated e)
        {
            this.AggregateId = e.AggregateId;
        }

        void When(FormatAddedToDocument e)
        {
            this.Formats.Add(e.FormatValue, e.FileId);
        }

        void When(DocumentFormatHasBeenDeleted e)
        {
            this.Formats.Remove(e.FormatValue);
        }

        public bool HasFormat(FormatValue formatValue)
        {
            return Formats.ContainsKey(formatValue);
        }
    }
}