using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ProcessingPipeline;

namespace Jarvis.DocumentStore.Core.ReadModel
{
    public class DocumentReadModel : AbstractReadModel<DocumentId>
    {
        public IDictionary<DocumentFormat, FileId> Formats { get; private set; }
        public IDictionary<FileAlias, FileNameWithExtension> Aliases { get; private set; }
        public int FormatsCount { get; set; }
        public int AliasesCount { get; set; }

        public DocumentReadModel(DocumentId id, FileId fileId, FileAlias alias, FileNameWithExtension fileName)
        {
            this.Formats = new Dictionary<DocumentFormat, FileId>();
            this.Aliases = new Dictionary<FileAlias, FileNameWithExtension>();
            
            this.Id = id;

            AddFormat(new DocumentFormat(DocumentFormats.Original), fileId);
            AddAlias(alias, fileName);
        }

        public void AddFormat(DocumentFormat format, FileId fileId)
        {
            this.Formats[format] = fileId;
        }

        public void AddAlias(FileAlias alias, FileNameWithExtension fileName)
        {
            this.Aliases[alias] = fileName;
        }

        public FileNameWithExtension GetFileName(FileAlias @alias)
        {
            return this.Aliases[alias];
        }

        public FileId GetFormatFileId(DocumentFormat format)
        {
            return this.Formats[format];
        }
    }
}
