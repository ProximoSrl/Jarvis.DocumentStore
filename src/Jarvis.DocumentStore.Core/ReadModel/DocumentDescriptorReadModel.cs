using System.Collections.Generic;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.ReadModel;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using DocumentFormat = Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.DocumentFormat;
using DocumentHandle = Jarvis.DocumentStore.Core.Model.DocumentHandle;
using System;

namespace Jarvis.DocumentStore.Core.ReadModel
{
    public class DocumentDescriptorReadModel : AbstractReadModel<DocumentDescriptorId>
    {
        public class FormatInfo
        {
            public BlobId BlobId { get; private set; }
            public PipelineId PipelineId { get; private set; }

            public FormatInfo(BlobId blobId, PipelineId pipelineId)
            {
                BlobId = blobId;
                PipelineId = pipelineId;
            }
        }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public IDictionary<DocumentFormat, FormatInfo> Formats { get; private set; }
        public HashSet<DocumentHandle> Documents { get; private set; }

        public FileHash Hash { get; set; }        
        public int FormatsCount { get; set; }
        public long SequenceNumber { get; set; }

        /// <summary>
        /// True when document descriptor has been deduplicated and it is 
        /// officially created
        /// </summary>
        public Boolean Created { get; set; }

        public HashSet<DocumentAttachmentReadModel> Attachments { get; private set; }

        public DocumentDescriptorReadModel(
            Int64 sequenceNumber,
            DocumentDescriptorId id, 
            BlobId blobId)
        {
            this.Formats = new Dictionary<DocumentFormat, FormatInfo>();
            this.Documents = new HashSet<DocumentHandle>();
            this.Id = id;
            this.SequenceNumber = sequenceNumber;
            AddFormat(PipelineId.Null, new DocumentFormat(DocumentFormats.Original), blobId);
            Attachments = new HashSet<DocumentAttachmentReadModel>(
                DocumentAttachmentReadModel.Comparer.Default);
        }

        public void AddFormat(PipelineId pipelineId, DocumentFormat format, BlobId blobId)
        {
            this.Formats[format] = new FormatInfo(blobId, pipelineId);
        }

        internal void RemoveFormat(DocumentFormat format)
        {
            if (this.Formats.ContainsKey(format))
                this.Formats.Remove(format);
        }

        public void AddHandle(DocumentHandle handle)
        {
            this.Documents.Add(handle);
        }

        public void Remove(DocumentHandle handle)
        {
            this.Documents.Remove(handle);
        }

        public BlobId GetFormatBlobId(DocumentFormat format)
        {
            FormatInfo formatInfo = null;
            if (Formats.TryGetValue(format, out formatInfo))
                return formatInfo.BlobId;

            return BlobId.Null;
        }

        public BlobId GetOriginalBlobId()
        {
            return GetFormatBlobId(DocumentFormats.Original);
        }

        internal void AddAttachments(DocumentHandle attachmentHandle, String attachmentPath)
        {
            Attachments.Add(new DocumentAttachmentReadModel(attachmentHandle, attachmentPath));
        }

      
    }

    public class DocumentAttachmentReadModel
    {

        public DocumentAttachmentReadModel()
        {

        }

        public DocumentAttachmentReadModel(DocumentHandle attachmentHandle, string attachmentPath)
        {
            Handle = attachmentHandle;
            RelativePath = attachmentPath;
        }

        /// <summary>
        /// Handle of the attachment.
        /// </summary>
        public DocumentHandle Handle { get; set; }

        /// <summary>
        /// Relative path of this attachment to the original handle
        /// </summary>
        public String RelativePath { get; set; }

        public class Comparer : IEqualityComparer<DocumentAttachmentReadModel> 
        {
            public static Comparer Default;

            static Comparer() 
            {
                Default = new Comparer();
            }

            public bool Equals(DocumentAttachmentReadModel x, DocumentAttachmentReadModel y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;

                return x.Handle.Equals(y.Handle);
            }

            public int GetHashCode(DocumentAttachmentReadModel obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
