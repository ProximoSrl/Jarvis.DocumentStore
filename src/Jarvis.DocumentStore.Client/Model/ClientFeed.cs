using Jarvis.DocumentStore.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Client.Model
{
    public class ClientFeed
    {

        public Int64 Id { get; set; }

        public String Handle { get; set; }

        public HandleStreamEventTypes EventType { get; set; }

        public String EventTypeDesc { get; set; }

        public String DocumentFormat { get; set; }

        public String FileName { get; set; }

        public String MimeType { get; set; }
    }

}
