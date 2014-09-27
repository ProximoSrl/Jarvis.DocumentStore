using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Host.Providers
{
    public class FileStoreMultipartStreamProvider : MultipartFormDataStreamProvider
    {
        readonly IFileStore _store;
        readonly FileId _fileId;
        readonly ConfigService _config;
        public string Filename { get; private set; }
        public bool IsInvalidFile { get; private set; }

        public FileStoreMultipartStreamProvider(
            IFileStore store, 
            FileId fileId,
            ConfigService config
        ) : base(Path.GetTempPath())
        {
            _store = store;
            _fileId = fileId;
            _config = config;
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            Filename  = headers.ContentDisposition.FileName;
//            var extension = Path.GetExtension(Filename.Replace('\"', ' ').Trim());

            if (!_config.IsFileAllowed(Filename))
            {
                IsInvalidFile = true;
                return new MemoryStream();
            }

            return _store.CreateNew(_fileId, Filename);
        }
    }
}
