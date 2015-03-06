using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Client.Model
{
    public class DocumentAttachmentsFat
    {
        readonly List<AttachmentInfo> _attachments;

        public List<AttachmentInfo> Attachments
        {
            get { return _attachments; }
        }

        public DocumentAttachmentsFat(List<AttachmentInfo> attachments)
        {
            _attachments = attachments;
        }

        public class AttachmentInfo 
        {
            public AttachmentInfo(String uri, String fileName, String attachmentPath)
                : this(uri, fileName, attachmentPath, "")
            {

            }

            public AttachmentInfo(String uri, String fileName, String attachmentPath, String relativePath)
            {
                Uri = uri;
                FileName = fileName;
                AttachmentPath = attachmentPath;
                RelativeFileName = relativePath + "/" + fileName;
            }

            public String Uri { get; private set; }

            public String FileName { get; private set; }

            public String RelativeFileName { get; private set; }

            public String AttachmentPath { get; private set; }
        }
    }
}
