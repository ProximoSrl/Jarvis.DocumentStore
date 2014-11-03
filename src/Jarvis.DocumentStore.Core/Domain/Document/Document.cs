using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Kernel.Engine;
using CQRS.Shared.ValueObjects;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    public class DocumentHandleInfo
    {
        public FileNameWithExtension FileName { get; private set; }
        public IDictionary<string, object> CustomData { get; private set; }
        public DocumentHandle Handle { get; private set; }

        public DocumentHandleInfo(
            DocumentHandle handle,
            FileNameWithExtension fileName,
            IDictionary<string, object> customData = null
            )
        {
            Handle = handle;
            FileName = fileName;
            CustomData = customData;
        }
    }

    public class Document : AggregateRoot<DocumentState>
    {
        public Document(DocumentState initialState)
            : base(initialState)
        {
        }

        public Document()
        {
        }

        public void Create(DocumentId id, FileId fileId, DocumentHandleInfo handleInfo)
        {
            ThrowIfDeleted();

            if (HasBeenCreated)
                throw new DomainException((IIdentity)id, "Already created");

            RaiseEvent(new DocumentCreated(id, fileId, handleInfo));
            RaiseEvent(new DocumentHandleAttached(handleInfo));
        }

        public void AddFormat(DocumentFormat documentFormat, FileId fileId, PipelineId createdBy)
        {
            ThrowIfDeleted();
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
                    InternalState.FileId,
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
