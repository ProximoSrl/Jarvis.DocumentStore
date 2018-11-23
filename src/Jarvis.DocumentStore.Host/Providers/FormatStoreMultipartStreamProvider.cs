﻿using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Storage;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
namespace Jarvis.DocumentStore.Host.Providers
{
    public class FormatStoreMultipartStreamProvider : MultipartFormDataStreamProvider
    {
        readonly IBlobStore _store;

        readonly DocumentFormat _format;

        public FileNameWithExtension Filename { get; private set; }

        public BlobId BlobId{
            get { return _writer != null ? _writer.BlobId : null; }
        }

        IBlobWriter _writer;
        public FormatStoreMultipartStreamProvider(
            IBlobStore store,
            DocumentFormat format
        ) : base(Path.GetTempPath())
        {
            _store = store;
            _format = format;
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            string fname = headers.ContentDisposition.FileName;
            if (fname == null)
                return new MemoryStream();

            Filename = new FileNameWithExtension(fname);
            _writer = _store.CreateNew(_format, Filename);

            return _writer.WriteStream;
        }
    }
}
