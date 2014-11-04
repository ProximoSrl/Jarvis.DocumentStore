using System.IO;
using System.Linq;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Storage
{
    public class BlobStoreInfo
    {
        public long Size { get; private set; }
        public long Files { get; private set; }

        public BlobStoreInfo(long size, long files)
        {
            Size = size;
            Files = files;
        }

        public BlobStoreInfo(params BlobStoreInfo[] info)
        {
            this.Size = info.Sum(x => x.Size);
            this.Files = info.Sum(x => x.Files);
        }
    }

    public interface IBlobStore
    {
        IBlobDescriptor GetDescriptor(BlobId blobId);
        void Delete(BlobId blobId);
        string Download(BlobId blobId, string folder);

        IBlobWriter CreateNew(DocumentFormat format, FileNameWithExtension fname);
        BlobId Upload(DocumentFormat format, string pathToFile);
        BlobId Upload(DocumentFormat format, FileNameWithExtension fileName, Stream sourceStrem);

        BlobStoreInfo GetInfo();
    }
}