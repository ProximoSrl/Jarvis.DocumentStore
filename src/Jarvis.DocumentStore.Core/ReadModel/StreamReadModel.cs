using System;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.ReadModel;

namespace Jarvis.DocumentStore.Core.ReadModel
{
    public class StreamReadModel : AbstractReadModel<Int64>
    {
        //public TenantId TenantId { get; set; }

        public String Handle { get; set; }

        public FormatInfo FormatInfo { get; set; }

        public DocumentDescriptorId DocumentId { get; set; }

        public FileNameWithExtension Filename { get; set; }

        public HandleStreamEventTypes EventType { get; set; }

        public DocumentCustomData DocumentCustomData { get; set; }

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
        HandleFormatUpdated = 5,
    }
}
