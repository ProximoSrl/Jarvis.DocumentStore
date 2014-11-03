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

        public void Create(DocumentId id, BlobId blobId, DocumentHandleInfo handleInfo)
        {
            ThrowIfDeleted();

            if (HasBeenCreated)
                throw new DomainException((IIdentity)id, "Already created");

            RaiseEvent(new DocumentCreated(id, blobId, handleInfo));
            RaiseEvent(new DocumentHandleAttached(handleInfo));
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

        public void Delete(DocumentHandle handle)
        {
            if (!InternalState.IsValidHandle(handle))
            {
                throw new DomainException(this.Id, string.Format("Document handle \"{0}\" is invalid", handle));
            }

            if (InternalState.HandleCount(handle) == 0)
            {
                Logger.DebugFormat("Handle {0} not found on {1}, skipping", handle, this.Id);
                return;
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

        public void Deduplicate(DocumentId documentId, DocumentHandleInfo handleInfo)
        {
            ThrowIfDeleted();
            RaiseEvent(new DocumentHandleAttached(handleInfo));
            RaiseEvent(new DocumentHasBeenDeduplicated(documentId, handleInfo.Handle));
        }

        void ThrowIfDeleted()
        {
            if (InternalState.HasBeenDeleted)
                throw new DomainException(this.Id, "Document has been deleted");
        }

        public void Process()
        {
            RaiseEvent(new DocumentQueuedForProcessing());
        }
    }
}
