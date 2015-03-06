using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Client.Model
{
    public class DocumentAttachmentsFat
    {
        readonly IDictionary<String, Uri> _attachments;
        public IDictionary<String, Uri> Attachments
        {
            get { return _attachments; }
        }

        public DocumentAttachmentsFat(IDictionary<String, Uri> attachments)
        {
            _attachments = attachments;
        }



    }
}
