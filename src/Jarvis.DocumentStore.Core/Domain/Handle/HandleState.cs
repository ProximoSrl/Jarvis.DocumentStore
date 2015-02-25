using System.Collections;
using System.Collections.Generic;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Handle.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Kernel.Engine;

namespace Jarvis.DocumentStore.Core.Domain.Handle
{
    public class HandleState : AggregateState
    {
        public HandleState(HandleId handleId, DocumentHandle handle) : this()
        {
            this.AggregateId = handleId;
            this.Handle = handle;
        }

        public HandleState()
        {
            _attachments = new List<DocumentHandle>();
        }

        void When(HandleInitialized e)
        {
            this.AggregateId = e.Id;
            this.Handle = e.Handle;
        }

        void When(HandleDeleted e)
        {
            MarkAsDeleted();
        }

        void When(HandleCustomDataSet e)
        {
            this.CustomData = e.CustomData;
        }

        void When(HandleFileNameSet e)
        {
            this.FileName = e.FileName;
        }

        void When(HandleLinked e)
        {
            Link(e.DocumentId);
        }

        void When(HandleHasNewAttachment e)
        {
            AddAttachment(e.Attachment);
        }

        public void Link(DocumentId documentId)
        {
            this.LinkedDocument = documentId;
        }

        public DocumentId LinkedDocument { get; private set; }

        public void MarkAsDeleted()
        {
            this.HasBeenDeleted = true;
        }

        public bool HasBeenDeleted { get; private set; }
        public HandleCustomData CustomData { get; private set; }
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
            _attachments.Remove(attachment);
        }

        public void SetCustomData(HandleCustomData data)
        {
            this.CustomData = data;
        }

        public void SetFileName(FileNameWithExtension fileNameWithExtension)
        {
            this.FileName = fileNameWithExtension;
        }
    }
}