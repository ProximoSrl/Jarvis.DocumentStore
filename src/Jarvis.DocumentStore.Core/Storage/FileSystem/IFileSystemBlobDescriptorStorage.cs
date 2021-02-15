using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Storage.FileSystem
{
    /// <summary>
    /// <para>
    ///  This is the interface used ONLY by the File System storage, it implements
    ///  storage descriptor, the class used to describe where the file is in file
    ///  system storage.
    /// </para>
    /// <para>In this first version the only reliable and performant class is the
    /// <see cref="MongodDbFileSystemBlobDescriptorStorage"/> that actually stores
    /// blob descriptor direcltly in a couple of dedicated mongodb storage.</para>
    /// </summary>
    internal interface IFileSystemBlobDescriptorStorage
    {
        void SaveDescriptor(FileSystemBlobDescriptor fileSystemBlobDescriptor);

        FileSystemBlobDescriptor FindOneById(BlobId blobId);

        BlobStoreInfo GetStoreInfo();

        void Delete(BlobId blobId);
    }
}

