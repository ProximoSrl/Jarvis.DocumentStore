using System.IO;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Storage
{
    public class BlobWriter : IBlobWriter
    {
        public BlobId BlobId { get; private set; }
        public Stream WriteStream { get; private set; }
        public FileNameWithExtension FileName { get; private set; }

        public BlobWriter(BlobId blobId, Stream writeStream, FileNameWithExtension fileName)
        {
            FileName = fileName;
            BlobId = blobId;
            WriteStream = writeStream;
        }

        public void Dispose()
        {
            WriteStream.Dispose();
        }
    }
}