namespace Jarvis.DocumentStore.Client
{
    public class UploadedDocumentResponse
    {
        public string Handle { get; set; }
        public string HashType { get; set; }
        public string Hash { get; set; }
        public string Uri { get; set; }
    }
}