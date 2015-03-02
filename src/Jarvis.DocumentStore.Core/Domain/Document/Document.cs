using System;
using System.Linq;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Kernel.Engine;
using Jarvis.NEventStoreEx.CommonDomainEx;
using Jarvis.NEventStoreEx.CommonDomainEx.Core;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    public class Document : AggregateRoot<DocumentState>
    {
        public Document()
        {
        }

        public void Initialize(DocumentId id, DocumentHandle handle)
        {

            ThrowIfDeleted();

            RaiseEvent(new DocumentInitialized(handle));
        }

        public void Link(DocumentDescriptorId documentId)
        {
            ThrowIfDeleted();

            if (InternalState.LinkedDocument != documentId){
                RaiseEvent(new DocumentLinked(
                    InternalState.Handle, 
                    documentId, 
                    InternalState.LinkedDocument,
                    InternalState.FileName
                ));
            }
        }

        public void SetFileName(FileNameWithExtension fileName)
        {
            ThrowIfDeleted();
            if (InternalState.FileName == fileName)
                return;

            RaiseEvent(new DocumentFileNameSet(InternalState.Handle, fileName));
        }

        public void SetCustomData(DocumentCustomData customData)
        {
            ThrowIfDeleted();

            if (DocumentCustomData.IsEquals(InternalState.CustomData, customData))
                return;

            RaiseEvent(new DocumentCustomDataSet(InternalState.Handle, customData));
        }

        public void Delete()
        {
            if (!InternalState.HasBeenDeleted)
            {
                if (InternalState.Attachments != null)
                {
                    foreach (var attachment in InternalState.Attachments.ToList())
                    {
                        RaiseEvent(new AttachmentDeleted(attachment));
                    }
                }
                RaiseEvent(new DocumentDeleted(InternalState.Handle, InternalState.LinkedDocument));
            }
        }

        public void DeleteAttachment(DocumentHandle attachmentHandle)
        {
            ThrowIfDeleted();

            if (!InternalState.Attachments.Contains(attachmentHandle))
                throw new DomainException(Id, "Cannot remove attachment " + attachmentHandle + ". Not found!");

            RaiseEvent(new AttachmentDeleted(attachmentHandle));
        }

        public void AddAttachment(DocumentHandle attachmentDocumentHandle)
        {
            ThrowIfDeleted();

            if (InternalState.Attachments.Contains(attachmentDocumentHandle))
                return;

            RaiseEvent(new DocumentHasNewAttachment(InternalState.Handle, attachmentDocumentHandle));
        }

        void ThrowIfDeleted()
        {
            if (InternalState.HasBeenDeleted)
                throw new DomainException((IIdentity)Id, "Handle has been deleted");
        }




       
    }
}
