using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Client.Model
{
    public class DocumentAttachments
    {
        readonly IDictionary<DocumentHandle, Uri> _attachments;
        public IDictionary<DocumentHandle, Uri> Attachments
        {
            get { return _attachments; }
        }

        public DocumentAttachments(IDictionary<DocumentHandle, Uri> attachments)
        {
            _attachments = attachments;
        }



    }
}
