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

        public void Initialize(DocumentHandle handle)
        {
            if (!HasBeenCreated)
            {
                RaiseEvent(new DocumentInitialized(handle, false));
            }
            else if (InternalState.HasBeenDeleted)
            {
                RaiseEvent(new DocumentInitialized(handle, true));
            }
            else
            {
                if (handle != InternalState.Handle)
                    throw new DomainException(Id, "Trying to initialize an initialized handle with different handle");
            }
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
                RaiseEvent(new DocumentDeleted(InternalState.Handle, InternalState.LinkedDocument));
            }
        }
        void ThrowIfDeleted()
        {
            if (InternalState.HasBeenDeleted)
                throw new DomainException((IIdentity)Id, "Handle has been deleted");
        }
    }
}
