namespace Jarvis.DocumentStore.Client.Model
{
    public class DocumentAttachments
    {
        private readonly ClientAttachmentInfo[] _attachments;

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
