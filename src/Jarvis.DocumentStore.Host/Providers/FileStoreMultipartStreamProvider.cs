using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Core.Support;

namespace Jarvis.DocumentStore.Host.Providers
{
    public class FileStoreMultipartStreamProvider : MultipartFormDataStreamProvider
    {
        readonly IBlobStore _store;
        readonly DocumentStoreConfiguration _config;
        public bool IsInvalidFile { get; private set; }
        public FileNameWithExtension Filename { get; private set; }

        public BlobId BlobId
        {
            get { return _writer != null ? _writer.BlobId : null; }
        }

        IBlobWriter _writer;
        public FileStoreMultipartStreamProvider(
            IBlobStore store,
            DocumentStoreConfiguration config
        )
            : base(Path.GetTempPath())
        {
            _store = store;
            _config = config;
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            string fname = headers.ContentDisposition.FileName;
            if (fname == null)
                return new MemoryStream();

            Filename = new FileNameWithExtension(fname);

            if (!_config.IsFileAllowed(Filename))
            {
                IsInvalidFile = true;
                return new MemoryStream();
            }

            _writer = _store.CreateNew(DocumentFormats.Original, Filename);

            return _writer.WriteStream;
        }
    }
}
