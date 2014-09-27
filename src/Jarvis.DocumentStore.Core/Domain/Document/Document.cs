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

        public void AddFormat(FormatId formatId, FileId fileId)
        {
            if (InternalState.HasFormat(formatId))
            {
                RaiseEvent(new DocumentFormatHasBeenUpdated(formatId, fileId));
            }
            else
            {
                RaiseEvent(new FormatAddedToDocument(formatId, fileId));
            }
        }

        public void DeleteFormat(FormatId formatId)
        {
            if (InternalState.HasFormat(formatId))
            {
                RaiseEvent(new DocumentFormatHasBeenDeleted(formatId));
            }
        }

        public void Delete()
        {
            RaiseEvent(new DocumentDeleted());
        }
    }
}
