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
            if(HasBeenCreated)
                throw new DomainException((IIdentity) id, "Already created");

            RaiseEvent(new DocumentCreated(id, fileId));
        }

        public void AddFormat(DocumentFormat documentFormat, FileId fileId)
        {
            if (InternalState.HasFormat(documentFormat))
            {
                RaiseEvent(new DocumentFormatHasBeenUpdated(documentFormat, fileId));
            }
            else
            {
                RaiseEvent(new FormatAddedToDocument(documentFormat, fileId));
            }
        }

        public void DeleteFormat(DocumentFormat documentFormat)
        {
            if (InternalState.HasFormat(documentFormat))
            {
                RaiseEvent(new DocumentFormatHasBeenDeleted(documentFormat));
            }
        }

        public void Delete()
        {
            RaiseEvent(new DocumentDeleted());
        }
    }
}
