using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Host.Model
{
    public class DocumentInfo
    {
        public String DocumentHandle { get; set; }

        public List<DocumentFormatInfo> Formats { get; set; }
    }

    public class DocumentFormatInfo
    {
        public String FormatType { get; set; }

        public String FormatUrl { get; set; }
    }
}
