using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.Services;
using Jarvis.ImageService.Core.Storage;

namespace Jarvis.ImageService.Core.Http
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
