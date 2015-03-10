using System;
using System.Linq;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Kernel.Engine;
using Jarvis.NEventStoreEx.CommonDomainEx;
using Jarvis.NEventStoreEx.CommonDomainEx.Core;
using Jarvis.DocumentStore.Core.Support;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor
{
    public class DocumentDescriptor : AggregateRoot<DocumentDescriptorState>
    {
        public DocumentDescriptor()
        {
        }

        public IDocumentFormatTranslator DocumentFormatTranslator { get; set; }

        public void Initialize(BlobId blobId, DocumentHandleInfo handleInfo, FileHash hash, String fileName)
        {
            ThrowIfDeleted();

            if (HasBeenCreated)
                throw new DomainException(Id, "Already initialized");

            RaiseEvent(new DocumentDescriptorInitialized(blobId, handleInfo, hash));

            var knownFormat = DocumentFormatTranslator.GetFormatFromFileName(fileName);
            if (knownFormat != null)
                RaiseEvent(new FormatAddedToDocumentDescriptor(knownFormat, blobId, null));
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
                RaiseEvent(new FormatAddedToDocumentDescriptor(documentFormat, blobId, createdBy));
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
                RaiseEvent(new DocumentDescriptorDeleted(
                    InternalState.BlobId,
                    InternalState.Formats.Select(x => x.Value).ToArray()
                ));
            }
        }

        public void Deduplicate(DocumentDescriptorId otherDocumentId, DocumentHandle handle, FileNameWithExtension fileName)
        {
            ThrowIfDeleted();
            RaiseEvent(new DocumentDescriptorHasBeenDeduplicated(otherDocumentId, handle, fileName));
            Attach(handle);
        }

        void ThrowIfDeleted()
        {
            if (InternalState.HasBeenDeleted)
                throw new DomainException(this.Id, "Document has been deleted");
        }

        public void Create(DocumentHandle handle)
        {
            if (InternalState.Created)
                throw new DomainException(this.Id, "Already created");
            RaiseEvent(new DocumentDescriptorCreated(InternalState.BlobId, handle));
            Attach(handle);
        }


    }
}
