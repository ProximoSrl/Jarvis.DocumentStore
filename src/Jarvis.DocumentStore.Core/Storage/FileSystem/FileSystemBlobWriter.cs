using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Model;
using MongoDB.Driver;
using Castle.Core.Logging;
using System.Security.Cryptography;
using Jarvis.Framework.Shared.Helpers;

namespace Jarvis.DocumentStore.Core.Storage.FileSystem
{
#pragma warning disable S3881 
    // "IDisposable" should be implemented correctly
    /// <summary>
    /// This is probably not the optimal use of this interface
    /// because we need not only to store the data inside the blob
    /// but we need also to manage the descriptor
    /// </summary>
    internal class FileSystemBlobWriter : IBlobWriter
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
    {
        private readonly IMongoCollection<FileSystemBlobDescriptor> _blobDescriptorCollection;
        private readonly ILogger _logger;
        private readonly FileSystemBlobDescriptor _descriptor;
        private readonly String _destinationFileName;
        private readonly FileSystemBlobStoreWritableStream _writableStream;

        public FileSystemBlobWriter(
            BlobId blobId,
            FileNameWithExtension fileName,
            String destinationFileName,
            IMongoCollection<FileSystemBlobDescriptor> blobDescriptorCollection,
            ILogger logger)
        {
            BlobId = blobId;
            FileName = fileName;
            _blobDescriptorCollection = blobDescriptorCollection;
            _logger = logger;

            _descriptor = new FileSystemBlobDescriptor()
            {
                BlobId = BlobId,
                FileNameWithExtension = FileName,
                Timestamp = DateTime.Now,
                ContentType = MimeTypes.GetMimeType(FileName)
            };
            _destinationFileName = destinationFileName;
            _blobDescriptorCollection.Save(_descriptor, _descriptor.BlobId);

            //Create a wrapper of the stream
            var originalStream = new FileStream(destinationFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            originalStream.SetLength(0);
            _writableStream = new FileSystemBlobStoreWritableStream(originalStream, _descriptor, _blobDescriptorCollection, this);
        }

        public BlobId BlobId { get; }

        public FileNameWithExtension FileName { get; }

        public Stream WriteStream
        {
            get { return _writableStream; }
        }

        public Boolean Disposed { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_writableStream.Disposed)
                {
                    _writableStream.Dispose();
                }
            }
            Disposed = true;
        }
    }
}
