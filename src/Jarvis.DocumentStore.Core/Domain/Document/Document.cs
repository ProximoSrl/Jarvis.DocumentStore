using System;
using System.Collections.Generic;
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

        public void Create(DocumentId id, FileId fileId, DocumentHandle handle, FileNameWithExtension fileName, IDictionary<string, object> customData = null)
        {
            if (HasBeenCreated)
                throw new DomainException((IIdentity)id, "Already created");

            RaiseEvent(new DocumentCreated(id, fileId, handle, fileName, customData));
        }

        public void AddFormat(DocumentFormat documentFormat, FileId fileId, PipelineId createdBy)
        {
            if (InternalState.HasFormat(documentFormat))
            {
                RaiseEvent(new DocumentFormatHasBeenUpdated(documentFormat, fileId, createdBy));
            }
            else
            {
                RaiseEvent(new FormatAddedToDocument(documentFormat, fileId, createdBy));
            }
        }

        public void DeleteFormat(DocumentFormat documentFormat)
        {
            if (InternalState.HasFormat(documentFormat))
            {
                RaiseEvent(new DocumentFormatHasBeenDeleted(documentFormat));
            }
        }

        public void Delete(DocumentHandle handle)
        {
            RaiseEvent(new DocumentDeleted(
                InternalState.FileId,
                InternalState.Formats.Select(x => x.Value).ToArray()
            ));
        }

        public void Deduplicate(DocumentId documentId, DocumentHandle handle, FileNameWithExtension fileName)
        {
            RaiseEvent(new DocumentHasBeenDeduplicated(documentId, handle, fileName));
        }
    }
}
