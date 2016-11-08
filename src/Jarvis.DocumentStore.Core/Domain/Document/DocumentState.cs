using System.Collections.Generic;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Kernel.Engine;
using System;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace Jarvis.DocumentStore.Core.Domain.Document
{

    public class DocumentState : AggregateState
    {
        public DocumentDescriptorId LinkedDocument { get; private set; }
        public bool HasBeenDeleted { get; private set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public DocumentCustomData CustomData { get; private set; }
        public DocumentHandle Handle { get; private set; }
        public FileNameWithExtension FileName { get; private set; }

        public DocumentState(DocumentHandle handle) : this()
        {
            this.Handle = handle;
        }

        public DocumentState()
        {

        }

        protected override object DeepCloneMe()
        {
            DocumentState cloned = new DocumentState();
            cloned.LinkedDocument = this.LinkedDocument;
            cloned.HasBeenDeleted = this.HasBeenDeleted;
            cloned.Handle = this.Handle;
            if (FileName != null)
                cloned.FileName = FileName.Clone();
            if (CustomData != null)
                cloned.CustomData = CustomData.Clone();
            return cloned;
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

        public void MarkAsDeleted()
        {
            this.HasBeenDeleted = true;
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