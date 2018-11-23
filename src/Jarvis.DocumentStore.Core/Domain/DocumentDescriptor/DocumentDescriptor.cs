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
    public class DocumentDescriptorDeletedException : DomainException
    {
        public DocumentDescriptorDeletedException(IIdentity id, String message) :
            base(id, $"Cannot execute operation on a Deleted document descriptor:{id} - {message}")
        {
        }
    }

    public class DocumentDescriptor : AggregateRoot<DocumentDescriptorState>
    {
        public DocumentDescriptor()
        {
        }

        public IDocumentFormatTranslator DocumentFormatTranslator { get; set; }

        public void Initialize(
            BlobId blobId,
            DocumentHandleInfo handleInfo,
            FileHash hash,
            String fileName)
        {
            ThrowIfDeleted(String.Format("Initialize with blob {0} and fileName {1}", blobId, fileName));

            if (HasBeenCreated)
                throw new DomainException(Id, "Already initialized");

            RaiseEvent(new DocumentDescriptorInitialized(blobId, handleInfo, hash));

            var knownFormat = DocumentFormatTranslator.GetFormatFromFileName(fileName);
            if (knownFormat != null)
                RaiseEvent(new FormatAddedToDocumentDescriptor(knownFormat, blobId, null));
        }

        public void InitializeAsAttach(
           BlobId blobId,
           DocumentHandleInfo handleInfo,
           FileHash hash,
           String fileName,
           DocumentDescriptorId fatherDocumentDescriptorId)
        {
            ThrowIfDeleted(String.Format("Initialize as attach with blob {0} and fileName {1}", blobId, fileName));

            if (HasBeenCreated)
                throw new DomainException(Id, "Already initialized");

            RaiseEvent(new DocumentDescriptorInitialized(blobId, handleInfo, hash, fatherDocumentDescriptorId));

            var knownFormat = DocumentFormatTranslator.GetFormatFromFileName(fileName);
            if (knownFormat != null)
                RaiseEvent(new FormatAddedToDocumentDescriptor(knownFormat, blobId, null));
        }

        public void AddFormat(DocumentFormat documentFormat, BlobId blobId, PipelineId createdBy)
        {
            ThrowIfDeleted(String.Format("Add format {0} and blob {1} - CreatedBy {2}", documentFormat, blobId, createdBy));
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
            ThrowIfDeleted(String.Format("Delete format {0}", documentFormat));
            if (InternalState.HasFormat(documentFormat))
            {
                RaiseEvent(new DocumentFormatHasBeenDeleted(documentFormat));
            }
        }

        public void Attach(DocumentHandleInfo handleInfo)
        {
            if (!InternalState.IsValidHandle(handleInfo.Handle))
                RaiseEvent(new DocumentHandleAttached(handleInfo.Handle));
        }

        public void Delete(DocumentHandle handle)
        {
            if (handle != DocumentHandle.Empty)
            {
                if (InternalState.IsValidHandle(handle))
                {
                    RaiseEvent(new DocumentHandleDetached(handle));
                }
                else
                {
                    Logger.WarnFormat("Invalid handle {0} on {1}", handle, this.Id);
                }
            }

            if (!InternalState.HasActiveHandles())
            {
                RaiseEvent(new DocumentDescriptorDeleted(
                    InternalState.BlobId,
                    InternalState.Formats.Select(x => x.Value).ToArray(),
                    InternalState.Attachments.ToArray()
                ));
            }
        }

        /// <summary>
        /// This DocumentDescriptor has the same content of another <see cref="DocumentDescriptor"/>
        /// this operation mark the current document as owner of the handle of duplicated document
        /// descriptor
        /// </summary>
        /// <param name="otherDocumentDescriptorId"></param>
        /// <param name="handle"></param>
        /// <param name="fileName"></param>
        public void Deduplicate(
            DocumentDescriptorId otherDocumentDescriptorId,
            DocumentHandleInfo handleInfo)
        {
            ThrowIfDeleted(String.Format("Deduplicate with {0}", otherDocumentDescriptorId));
            RaiseEvent(new DocumentDescriptorHasBeenDeduplicated(
                otherDocumentDescriptorId,
                handleInfo));
            Attach(handleInfo);
        }

        public void Create(DocumentHandleInfo handleInfo)
        {
            if (InternalState.Created)
            {
                if (handleInfo.Equals(InternalState.CreationDocumentHandleInfo))
                {
                    //idempotency, already initialized
                    return;
                }
                throw new DomainException(this.Id, "Already created");
            }

            RaiseEvent(new DocumentDescriptorCreated(InternalState.BlobId, handleInfo));
            Attach(handleInfo);
        }

        public void AddAttachment(DocumentHandle attachmentDocumentHandle, String attachmentPath)
        {
            ThrowIfDeleted(String.Format("Add attachment {0} with path {1}", attachmentDocumentHandle, attachmentPath));
            if (InternalState.Attachments.Contains(attachmentDocumentHandle)) return;

            RaiseEvent(new DocumentDescriptorHasNewAttachment(attachmentDocumentHandle, attachmentPath));
        }

        #region Helpers

        private void ThrowIfDeleted(String operation)
        {
            if (InternalState.HasBeenDeleted)
                throw new DocumentDescriptorDeletedException(this.Id, operation);
        }

        #endregion
    }
}
