using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace Jarvis.DocumentStore.Core.ReadModel
{
    public class DocumentReadModel : AbstractReadModel<DocumentId>
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
        public HashSet<DocumentHandle> Handles { get; private set; }

        public FileHash Hash { get; set; }        
        public int FormatsCount { get; set; }

        public DocumentReadModel(DocumentId id, BlobId blobId)
        {
            this.Formats = new Dictionary<DocumentFormat, FormatInfo>();
            this.Handles = new HashSet<DocumentHandle>();
            this.Id = id;

            AddFormat(PipelineId.Null, new DocumentFormat(DocumentFormats.Original), blobId);
        }

        public void AddFormat(PipelineId pipelineId, DocumentFormat format, BlobId blobId)
        {
            this.Formats[format] = new FormatInfo(blobId, pipelineId);
        }

        public void AddHandle(DocumentHandle handle)
        {
            this.Handles.Add(handle);
        }

        public void Remove(DocumentHandle handle)
        {
            this.Handles.Remove(handle);
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
