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
        /// this is used from the various workers it is the id of the job that produces other content
        /// for DocumentId associated to that job
        /// </summary>
        public String JobId { get; set; }

        /// <summary>
        /// This is used to specify the queue <see cref="JobId" /> belongs to.
        /// </summary>
        public String QueueName { get; set; }

        /// <summary>
        /// this is used if the caller want to add a format to a given handle.
        /// </summary>
        public DocumentHandle DocumentHandle{ get; set; }

        public DocumentFormat Format { get; set; }

        public String CreatedById { get; set; }
    }

    public class AddFormatFromFileToDocumentModel : AddFormatToDocumentModel
    {
        public String PathToFile { get; set; }
    }

    public class AddFormatFromObjectToDocumentModel : AddFormatToDocumentModel
    {
        /// <summary>
        /// If content is some serialized in-memory object (json) I can simply send the
        /// content as a simple string.
        /// If this parameter is used <see cref="AddFormatFromFileToDocumentModel.PathToFile" /> is ignored.
        /// </summary>
        public String StringContent { get; set; }
    }
}
