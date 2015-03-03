using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Shared.Model;

namespace Jarvis.DocumentStore.Host.Model
{
    public class FeedForStreamReadModel
    {
        public String Handle { get; set; }

        public HandleStreamEventTypes EventType { get; set; }

        public String EventTypeDesc { get; set; }

        public String DocumentFormat { get; set; }

        public String FileName { get; set; }

        public String MimeType { get; set; }



        public FeedForStreamReadModel(StreamReadModel original)
        {
            Handle = original.Handle;
            EventType = original.EventType;
            EventTypeDesc = original.EventType.ToString();
            if (original.FormatInfo != null)
            {
                DocumentFormat = original.FormatInfo.DocumentFormat;
            }
            if (original.Filename != null)
            {
                FileName = original.Filename;
                MimeType = MimeTypes.GetMimeType(original.Filename);
            }
            
            
        }
    }
}
