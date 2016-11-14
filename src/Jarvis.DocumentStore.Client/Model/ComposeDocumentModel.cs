using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Client.Model
{
    public class ComposeDocumentsModel
    {
        public DocumentHandle[] DocumentList { get; set; }

        public DocumentHandle ResultingDocumentHandle { get; set; }

        public String ResultingDocumentFileName { get; set; }
    }
}
