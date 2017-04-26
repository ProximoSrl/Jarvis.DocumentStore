using System.IO;
using Jarvis.DocumentStore.Core.Model;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
namespace Jarvis.DocumentStore.Core.Storage
{
    /// <summary>
    /// Abstract the writing on a blob.
    /// </summary>
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