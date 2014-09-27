using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Kernel.Engine;
using CQRS.Shared.ValueObjects;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;
using Microsoft.SqlServer.Server;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    public class Document : AggregateRoot<DocumentState>
    {
        public Document(DocumentState initialState)
            : base(initialState)
        {
        }

        public Document()
        {
        }

        public void Create(DocumentId id, FileId fileId)
        {
            RaiseEvent(new DocumentCreated(id, fileId));
        }

        public void AddFormat(FormatValue formatValue, FileId fileId)
        {
            if (InternalState.HasFormat(formatValue))
            {
                RaiseEvent(new DocumentFormatHasBeenUpdated(formatValue, fileId));
            }
            else
            {
                RaiseEvent(new FormatAddedToDocument(formatValue, fileId));
            }
        }

        public void DeleteFormat(FormatValue formatValue)
        {
            if (InternalState.HasFormat(formatValue))
            {
                RaiseEvent(new DocumentFormatHasBeenDeleted(formatValue));
            }
        }

        public void Delete()
        {
            RaiseEvent(new DocumentDeleted());
        }
    }
}
