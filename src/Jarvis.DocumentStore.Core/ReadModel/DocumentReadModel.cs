using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;

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

        public IDictionary<DocumentFormat, FormatInfo> Formats { get; private set; }
        public IDictionary<FileHandle, FileNameWithExtension> Handles { get; private set; }
        public int FormatsCount { get; set; }
        public int HandlesCount { get; set; }

        public DocumentReadModel(DocumentId id, FileId fileId, FileHandle handle, FileNameWithExtension fileName)
        {
            this.Formats = new Dictionary<DocumentFormat, FormatInfo>();
            this.Handles = new Dictionary<FileHandle, FileNameWithExtension>();
            
            this.Id = id;

            AddFormat(PipelineId.Null, new DocumentFormat(DocumentFormats.Original), fileId);
            AddHandle(handle, fileName);
        }

        public void AddFormat(PipelineId pipelineId, DocumentFormat format, FileId fileId)
        {
            this.Formats[format] = new FormatInfo(fileId, pipelineId);
        }

        public void AddHandle(FileHandle handle, FileNameWithExtension fileName)
        {
            this.Handles[handle] = fileName;
        }

        public FileNameWithExtension GetFileName(FileHandle handle)
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
