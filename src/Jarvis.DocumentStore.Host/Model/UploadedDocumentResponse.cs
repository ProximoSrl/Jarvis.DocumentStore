using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Host.Model
{
    public class UploadedDocumentResponse
    {
        public string Handle { get; set; }
        public string HashType { get; set; }
        public string Hash { get; set; }
        public string Uri { get; set; }
    }
}
