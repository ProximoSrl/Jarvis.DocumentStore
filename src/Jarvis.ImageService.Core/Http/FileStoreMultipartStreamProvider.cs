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
using MongoDB.Driver;

namespace Jarvis.ImageService.Core.Http
{
    public class FileStoreMultipartStreamProvider : MultipartFormDataStreamProvider
    {
        readonly IFileStore _store;
        readonly string _resourceId;

        public FileStoreMultipartStreamProvider(IFileStore store, string resourceId) : base(Path.GetTempPath())
        {
            _store = store;
            _resourceId = resourceId;
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            var fname = headers.ContentDisposition.FileName;
            return _store.CreateNew(fname, _resourceId);
        }
    }
}
