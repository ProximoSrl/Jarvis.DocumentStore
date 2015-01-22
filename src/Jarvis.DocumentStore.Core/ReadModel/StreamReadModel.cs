using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Handle;
using Jarvis.DocumentStore.Core.Model;
using MongoDB.Bson.Serialization.Attributes;
using CQRS.Shared.MultitenantSupport;

namespace Jarvis.DocumentStore.Core.ReadModel
{
    public class StreamReadModel : AbstractReadModel<Int64>
    {
        //public TenantId TenantId { get; set; }

        public String Handle { get; set; }

        public FormatInfo FormatInfo { get; set; }

        public String Pi { get; set; }

        public DocumentId DocumentId { get; set; }

        public FileNameWithExtension Filename { get; set; }

        public HandleStreamEventTypes EventType { get; set; }
    }

    public class FormatInfo
    {
        public DocumentFormat DocumentFormat { get; set; }

        public PipelineId PipelineId { get; set; }

        public BlobId BlobId { get; set; }
    }

    public enum HandleStreamEventTypes
    {
        Unknown = 0,
        HandleInitialized = 1,
        HandleDeleted = 2,
        HandleHasNewFormat = 3,
        HandleFileNameSet = 4,
        HandleFormatUpdated = 3,
    }
}
