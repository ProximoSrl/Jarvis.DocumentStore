using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Storage.FileSystem
{
    internal interface IFileSystemBlobDescriptorStorage
    {
        void SaveDescriptor(FileSystemBlobDescriptor fileSystemBlobDescriptor);

        FileSystemBlobDescriptor FindOneById(BlobId blobId);

        BlobStoreInfo GetStoreInfo();

        void Delete(BlobId blobId);
    }
}

