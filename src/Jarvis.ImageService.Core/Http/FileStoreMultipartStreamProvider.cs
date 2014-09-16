using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Storage;

namespace Jarvis.ImageService.Core.Http
{
    public class FileStoreMultipartStreamProvider : MultipartFormDataStreamProvider
    {
        readonly IFileStore _store;
        readonly string _resourceId;
        public string Filename { get; private set; }
        public string UnsupportedExtension { get; private set; }

        public FileStoreMultipartStreamProvider(IFileStore store, string resourceId) : base(Path.GetTempPath())
        {
            _store = store;
            _resourceId = resourceId;
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            Filename  = headers.ContentDisposition.FileName;
            var extension = Path.GetExtension(Filename.Replace('\"', ' ').Trim());
            
            if (extension.ToLowerInvariant() != ".pdf")
            {
                UnsupportedExtension = extension;
                return new MemoryStream();
            }

            return _store.CreateNew(_resourceId, Filename);
        }
    }
}
