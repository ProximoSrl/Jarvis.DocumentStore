using System.Collections.Generic;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Kernel.Engine;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    public class DocumentState : AggregateState
    {
        public DocumentState(DocumentId documentId, DocumentHandle handle) : this()
        {
            this.AggregateId = documentId;
            this.Handle = handle;
        }

        public DocumentState()
        {
            _attachments = new List<DocumentHandle>();
        }

        void When(DocumentInitialized e)
        {
            this.AggregateId = e.Id;
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

        void When(DocumentHasNewAttachment e)
        {
            AddAttachment(e.Attachment);
        }

        void When(AttachmentDeleted e)
        {
            RemoveAttachment(e.Handle);
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

        public IEnumerable<DocumentHandle> Attachments {
            get { return _attachments.AsReadOnly(); }
        }

        private List<DocumentHandle> _attachments;

        public void AddAttachment(DocumentHandle attachment)
        {
            _attachments.Add(attachment);
        }

        public void RemoveAttachment(DocumentHandle attachment)
        {
            if (_attachments.Contains(attachment)) _attachments.Remove(attachment);
        }

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