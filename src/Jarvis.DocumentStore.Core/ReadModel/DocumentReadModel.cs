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
            public FileId FileId { get; private set; }
            public PipelineId PipelineId { get; private set; }

            public FormatInfo(FileId fileId, PipelineId pipelineId)
            {
                FileId = fileId;
                PipelineId = pipelineId;
            }
        }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public IDictionary<DocumentFormat, FormatInfo> Formats { get; private set; }
        public List<DocumentHandleInfo> Handles { get; private set; }
        public FileHash Hash { get; set; }        
        public int FormatsCount { get; set; }
        public int HandlesCount { get; set; }

        public DocumentReadModel(DocumentId id, FileId fileId)
        {
            this.Formats = new Dictionary<DocumentFormat, FormatInfo>();
            this.Handles = new List<DocumentHandleInfo>();

            this.Id = id;

            AddFormat(PipelineId.Null, new DocumentFormat(DocumentFormats.Original), fileId);
        }

        public void AddFormat(PipelineId pipelineId, DocumentFormat format, FileId fileId)
        {
            this.Formats[format] = new FormatInfo(fileId, pipelineId);
        }

        public void AddHandle(DocumentHandleInfo handleInfo)
        {
            this.Handles.Add(handleInfo);
        }

        public void RemoveHandle(DocumentHandle handle)
        {
            this.Handles.RemoveAll(x=>x.Handle == handle);
        }

        public FileNameWithExtension GetFileName(DocumentHandle handle)
        {
            var info = this.Handles.Single(x => x.Handle == handle);
            return info.FileName;
        }

        public FileId GetFormatFileId(DocumentFormat format)
        {
            FormatInfo formatInfo = null;
            if (Formats.TryGetValue(format, out formatInfo))
                return formatInfo.FileId;

            return FileId.Null;
        }

        public FileId GetOriginalFileId()
        {
            return GetFormatFileId(DocumentFormats.Original);
        }
    }
}
