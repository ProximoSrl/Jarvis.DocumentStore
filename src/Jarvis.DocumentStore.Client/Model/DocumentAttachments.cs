using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Client.Model
{
    public class DocumentAttachments
    {
        readonly ClientAttachmentInfo[] _attachments;
        public ClientAttachmentInfo[] Attachments
        {
            get { return _attachments; }
        }

        public DocumentAttachments(ClientAttachmentInfo[] attachments)
        {
            _attachments = attachments;
        }



    }
}
