using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Kernel.Engine;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.NEventStoreEx.CommonDomainEx;
using Jarvis.NEventStoreEx.CommonDomainEx.Core;

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

        public IDocumentFormatTranslator DocumentFormatTranslator { get; set; }

        public void Create(DocumentId id, BlobId blobId, DocumentHandleInfo handleInfo, FileHash hash, String fileName)
        {
            ThrowIfDeleted();

            if (HasBeenCreated)
                throw new DomainException((IIdentity)id, "Already created");

            RaiseEvent(new DocumentCreated(id, blobId, handleInfo, hash));

            var knownFormat = DocumentFormatTranslator.GetFormatFromFileName(fileName);
            if (knownFormat != null)
                RaiseEvent(new FormatAddedToDocument(knownFormat, blobId, null));
        }

        public void CreateAsAttach(
            DocumentId id,
            BlobId blobId,
            DocumentHandleInfo attachHandle,
            DocumentHandle fatherHandle,
            FileHash hash,
            String fileName)
        {
            ThrowIfDeleted();

            if (HasBeenCreated)
                throw new DomainException((IIdentity)id, "Already created");

            RaiseEvent(new DocumentCreated(id, blobId, attachHandle, fatherHandle, hash));

            var knownFormat = DocumentFormatTranslator.GetFormatFromFileName(fileName);
            if (knownFormat != null)
                RaiseEvent(new FormatAddedToDocument(knownFormat, blobId, null));
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

        void Attach(DocumentHandle handle)
        {
            if (!InternalState.IsValidHandle(handle))
                RaiseEvent(new DocumentHandleAttached(handle));
        }

        public void Delete(DocumentHandle handle)
        {
            if (handle != DocumentHandle.Empty)
            {
                if (!InternalState.IsValidHandle(handle))
                {
                    throw new DomainException(this.Id, string.Format("Document handle \"{0}\" is invalid", handle));
                }

                RaiseEvent(new DocumentHandleDetached(handle));
            }

            if (!InternalState.HasActiveHandles())
            {
                RaiseEvent(new DocumentDeleted(
                    InternalState.BlobId,
                    InternalState.Formats.Select(x => x.Value).ToArray()
                ));
            }
        }

        public void Deduplicate(DocumentId otherDocumentId, DocumentHandle handle, FileNameWithExtension fileName)
        {
            ThrowIfDeleted();
            RaiseEvent(new DocumentHasBeenDeduplicated(otherDocumentId, handle, fileName));
            Attach(handle);
        }

        void ThrowIfDeleted()
        {
            if (InternalState.HasBeenDeleted)
                throw new DomainException(this.Id, "Document has been deleted");
        }

        public void Process(DocumentHandle handle)
        {
            RaiseEvent(new DocumentQueuedForProcessing(InternalState.BlobId, handle));
            Attach(handle);
        }


    }
}
