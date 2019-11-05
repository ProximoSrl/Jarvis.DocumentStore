using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Model;
using System;
using System.IO;

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
        private readonly ILogger _logger;
        private readonly FileSystemBlobDescriptor _descriptor;
        private readonly FileSystemBlobStoreWritableStream _writableStream;
        private readonly IFileSystemBlobDescriptorStorage _fileSystemBlobDescriptorStorage;

        public FileSystemBlobWriter(
            BlobId blobId,
            FileNameWithExtension fileName,
            String destinationFileName,
            IFileSystemBlobDescriptorStorage fileSystemBlobDescriptorStorage,
            ILogger logger)
        {
            BlobId = blobId;
            FileName = fileName;
            _logger = logger;

            _descriptor = new FileSystemBlobDescriptor()
            {
                BlobId = BlobId,
                FileNameWithExtension = FileName,
                Timestamp = DateTime.Now,
                ContentType = MimeTypes.GetMimeType(FileName)
            };

            //Create a wrapper of the stream
            var originalStream = new FileStream(destinationFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            originalStream.SetLength(0);
            _writableStream = new FileSystemBlobStoreWritableStream(originalStream, this);
            _writableStream.StreamClosed += WritableStreamClosed;
            _fileSystemBlobDescriptorStorage = fileSystemBlobDescriptorStorage;
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
            if (disposing && !_writableStream.Disposed)
            {
                //It is important to dispose the underling stream, so it will flush all content, will calculate MD5 and raise StreamClosedEvent.
                _writableStream.Dispose();
                _logger.DebugFormat("Persisting descriptor for blob {0}", _descriptor.BlobId);
                _writableStream.StreamClosed -= WritableStreamClosed;
            }
            Disposed = true;
        }

        private void WritableStreamClosed(
            object sender,
            FileSystemBlobStoreWritableStream.FileSystemBlobStoreWritableStreamClosedEventArgs e)
        {
            _descriptor.Md5 = e.Md5;
            _descriptor.Length = e.Length;

            _fileSystemBlobDescriptorStorage.SaveDescriptor(_descriptor);
        }
    }
}
