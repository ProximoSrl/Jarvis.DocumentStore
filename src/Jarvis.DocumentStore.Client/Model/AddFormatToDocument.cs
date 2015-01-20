using Jarvis.DocumentStore.Client.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Client.Model
{
    public class AddFormatToDocumentModel
    {
        /// <summary>
        /// this is used from the various workers, when they want to add a format to a specific document.
        /// </summary>
        public String DocumentId { get; set; }

        /// <summary>
        /// this is used if the caller want to add a format to a given handle.
        /// </summary>
        public DocumentHandle DocumentHandle{ get; set; }

        public DocumentFormat Format { get; set; }

        public String CreatedById { get; set; }

        public String PathToFile { get; set; }
    }
}
