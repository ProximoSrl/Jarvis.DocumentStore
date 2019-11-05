using Jarvis.DocumentStore.Core.Model;
using Newtonsoft.Json;
using System;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;

namespace Jarvis.DocumentStore.Core.Storage.FileSystem
{
    internal class FileSystemBlobDescriptorStore : IFileSystemBlobDescriptorStorage
    {
        private readonly DirectoryManager _directoryManager;

        internal FileSystemBlobDescriptorStore(DirectoryManager directoryManager)
        {
            _directoryManager = directoryManager ?? throw new ArgumentNullException(nameof(directoryManager));
        }

        public BlobStoreInfo GetStoreInfo()
        {
            //we have no way to calculate this with simple routine.
            return new BlobStoreInfo(0, 0);
        }

        public void Delete(BlobId blobId)
        {
            var descriptorLocalFileName = _directoryManager.GetDescriptorFileNameFromBlobId(blobId);
            if (File.Exists(descriptorLocalFileName))
                File.Delete(descriptorLocalFileName);
        }

        public FileSystemBlobDescriptor FindOneById(BlobId blobId)
        {
            if (blobId == null)
                throw new ArgumentNullException(nameof(blobId));

            var descriptorLocalFileName = _directoryManager.GetDescriptorFileNameFromBlobId(blobId);
            if (!File.Exists(descriptorLocalFileName))
                return null;

            var descriptor = JsonConvert.DeserializeObject<FileSystemBlobDescriptor>(File.ReadAllText(descriptorLocalFileName));
            descriptor.SetLocalFileName(_directoryManager.GetFileNameFromBlobId(blobId, descriptor.FileNameWithExtension));
            return descriptor;
        }

        public void SaveDescriptor(FileSystemBlobDescriptor fileSystemBlobDescriptor)
        {
            if (fileSystemBlobDescriptor == null)
                throw new ArgumentNullException(nameof(fileSystemBlobDescriptor));

            var fileName = _directoryManager.GetDescriptorFileNameFromBlobId(fileSystemBlobDescriptor.BlobId);
            File.WriteAllText(fileName, JsonConvert.SerializeObject(fileSystemBlobDescriptor));
        }
    }
}
