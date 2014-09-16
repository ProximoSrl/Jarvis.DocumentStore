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
    public class UnsupportedFileFormat : Exception
    {
        public UnsupportedFileFormat(string extension)
            :base(string.Format("Unsupported file {0}", extension))
        {
        }
    }

    public class FileStoreMultipartStreamProvider : MultipartFormDataStreamProvider
    {
        readonly IFileStore _store;
        readonly string _resourceId;
        public string Filename { get; private set; }
        public FileStoreMultipartStreamProvider(IFileStore store, string resourceId) : base(Path.GetTempPath())
        {
            _store = store;
            _resourceId = resourceId;
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            Filename  = headers.ContentDisposition.FileName;
            var extension = Path.GetExtension(Filename);
            
            if (extension == null || extension.ToLowerInvariant() != ".pdf")
            {
                throw new UnsupportedFileFormat(extension);
            }
            
            return _store.CreateNew(_resourceId, Filename);
        }
    }
}
