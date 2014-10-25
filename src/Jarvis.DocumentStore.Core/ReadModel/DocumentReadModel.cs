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
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public IDictionary<DocumentHandle, FileNameWithExtension> Handles { get; private set; }
        
        public int FormatsCount { get; set; }
        public int HandlesCount { get; set; }

        public HashSet<DocumentHandle>  MappedHandles { get; private set; }

        public DocumentReadModel(DocumentId id, FileId fileId, DocumentHandle handle, FileNameWithExtension fileName)
        {
            this.Formats = new Dictionary<DocumentFormat, FormatInfo>();
            this.Handles = new Dictionary<DocumentHandle, FileNameWithExtension>();
            this.MappedHandles = new HashSet<DocumentHandle>();

            this.Id = id;

            AddFormat(PipelineId.Null, new DocumentFormat(DocumentFormats.Original), fileId);
            AddHandle(handle, fileName);
        }

        public void AddFormat(PipelineId pipelineId, DocumentFormat format, FileId fileId)
        {
            this.Formats[format] = new FormatInfo(fileId, pipelineId);
        }

        public void AddHandle(DocumentHandle handle, FileNameWithExtension fileName)
        {
            this.Handles[handle] = fileName;
            this.MappedHandles.Add(handle);
        }

        public void RemoveHandle(DocumentHandle handle)
        {
            this.Handles.Remove(handle);
            this.MappedHandles.Remove(handle);
        }

        public FileNameWithExtension GetFileName(DocumentHandle handle)
        {
            return this.Handles[handle];
        }

        public FileId GetFormatFileId(DocumentFormat format)
        {
            FormatInfo formatInfo = null;
            if (Formats.TryGetValue(format, out formatInfo))
                return formatInfo.FileId;

            return FileId.Null;
        }
    }
}
