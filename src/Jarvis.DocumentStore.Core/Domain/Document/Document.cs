using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Kernel.Engine;
using CQRS.Shared.ValueObjects;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;

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

        public void Create(DocumentId id, BlobId blobId, DocumentHandleInfo handleInfo, FileHash hash)
        {
            ThrowIfDeleted();

            if (HasBeenCreated)
                throw new DomainException((IIdentity)id, "Already created");

            RaiseEvent(new DocumentCreated(id, blobId, handleInfo, hash));
            Attach(handleInfo.Handle);
        }

        public void AddFormat(DocumentFormat documentFormat, BlobId blobId, PipelineId createdBy)
        {
            ThrowIfDeleted();
            if (InternalState.HasFormat(documentFormat))
            {
                RaiseEvent(new DocumentFormatHasBeenUpdated(documentFormat, blobId, createdBy));
            }
            else
            {
                RaiseEvent(new FormatAddedToDocument(documentFormat, blobId, createdBy));
            }
        }

        public void DeleteFormat(DocumentFormat documentFormat)
        {
            ThrowIfDeleted();
            if (InternalState.HasFormat(documentFormat))
            {
                RaiseEvent(new DocumentFormatHasBeenDeleted(documentFormat));
            }
        }

        public void Attach(DocumentHandle handle)
        {
            if(!InternalState.IsValidHandle(handle))
                RaiseEvent(new DocumentHandleAttacched(handle));
        }

        public void Delete(DocumentHandle handle)
        {
            if (!InternalState.IsValidHandle(handle))
            {
                throw new DomainException(this.Id, string.Format("Document handle \"{0}\" is invalid", handle));
            }

            RaiseEvent(new DocumentHandleDetached(handle));

            if (!InternalState.HasActiveHandles())
            {
                RaiseEvent(new DocumentDeleted(
                    InternalState.BlobId,
                    InternalState.Formats.Select(x => x.Value).ToArray()
                ));
            }
        }

        public void Deduplicate(DocumentId documentId, DocumentHandle handle)
        {
            ThrowIfDeleted();
            RaiseEvent(new DocumentHasBeenDeduplicated(documentId, handle));
            Attach(handle);
        }

        void ThrowIfDeleted()
        {
            if (InternalState.HasBeenDeleted)
                throw new DomainException(this.Id, "Document has been deleted");
        }

        public void Process()
        {
            RaiseEvent(new DocumentQueuedForProcessing(InternalState.BlobId));
        }
    }
}
