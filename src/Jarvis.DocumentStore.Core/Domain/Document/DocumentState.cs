using System.Collections.Generic;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Kernel.Engine;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    public class DocumentState : AggregateState
    {
        public DocumentState(DocumentHandle handle) : this()
        {
            this.Handle = handle;
        }

        public DocumentState()
        {

        }

        void When(DocumentInitialized e)
        {
            this.Handle = e.Handle;
        }

        void When(DocumentDeleted e)
        {
            MarkAsDeleted();
        }

        void When(DocumentCustomDataSet e)
        {
            this.CustomData = e.CustomData;
        }

        void When(DocumentFileNameSet e)
        {
            this.FileName = e.FileName;
        }

        void When(DocumentLinked e)
        {
            Link(e.DocumentId);
        }

        public void Link(DocumentDescriptorId documentId)
        {
            this.LinkedDocument = documentId;
        }

        public DocumentDescriptorId LinkedDocument { get; private set; }

        public void MarkAsDeleted()
        {
            this.HasBeenDeleted = true;
        }

        public bool HasBeenDeleted { get; private set; }
        public DocumentCustomData CustomData { get; private set; }
        public DocumentHandle Handle { get; private set; }
        public FileNameWithExtension FileName { get; private set; }

        public void SetCustomData(DocumentCustomData data)
        {
            this.CustomData = data;
        }

        public void SetFileName(FileNameWithExtension fileNameWithExtension)
        {
            this.FileName = fileNameWithExtension;
        }
    }
}