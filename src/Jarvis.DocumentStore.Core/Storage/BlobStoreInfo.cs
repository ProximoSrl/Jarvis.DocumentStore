using System.Linq;

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
}