using System;
using System.Collections.Generic;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Shared.Model;
using Jarvis.Framework.Shared.ReadModel;

namespace Jarvis.DocumentStore.Core.ReadModel
{
    public class StreamReadModel : AbstractReadModel<Int64>
    {
        public String Handle { get; internal set; }

        public FormatInfo FormatInfo { get; internal set; }

        public DocumentDescriptorId DocumentDescriptorId { get; internal set; }

        public FileNameWithExtension Filename { get; internal set; }

        public HandleStreamEventTypes EventType { get; internal set; }

        public DocumentCustomData DocumentCustomData { get; internal set; }

        /// <summary>
        /// With attachment we need to store concept of attachment father
        /// and child. This dictionary contains all data that could be needed
        /// for a complete description of the event.
        /// </summary>
        public Dictionary<String, Object> EventData { get; private set; }

        public void AddEventData(String key, Object value)
        {
            if (EventData == null) EventData = new Dictionary<string, object>();
            EventData.Add(key, value);
        }
    }

    public class FormatInfo
    {
        public DocumentFormat DocumentFormat { get; set; }

        public PipelineId PipelineId { get; set; }

        public BlobId BlobId { get; set; }
    }


}
