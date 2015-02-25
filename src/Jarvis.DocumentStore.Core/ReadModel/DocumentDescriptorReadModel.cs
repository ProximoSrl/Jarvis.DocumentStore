using System.Collections.Generic;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.ReadModel;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using DocumentFormat = Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.DocumentFormat;
using DocumentHandle = Jarvis.DocumentStore.Core.Model.DocumentHandle;

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

        public DocumentDescriptorReadModel(DocumentDescriptorId id, BlobId blobId)
        {
            this.Formats = new Dictionary<DocumentFormat, FormatInfo>();
            this.Documents = new HashSet<DocumentHandle>();
            this.Id = id;
            this.SequenceNumber = id.Id;
            AddFormat(PipelineId.Null, new DocumentFormat(DocumentFormats.Original), blobId);
        }

        public void AddFormat(PipelineId pipelineId, DocumentFormat format, BlobId blobId)
        {
            this.Formats[format] = new FormatInfo(blobId, pipelineId);
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
    }
}
