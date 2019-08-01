using Jarvis.DocumentStore.Core.Model;
using Newtonsoft.Json;
using System;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;

namespace Jarvis.DocumentStore.Core.Storage.FileSystem
{
    internal class FileSystemBlobDescriptorStore
    {
        private readonly DirectoryManager _directoryManager;
        private readonly FileSystemBlobDescriptorStore _fileSystemBlobDescriptorStore;

        internal FileSystemBlobDescriptorStore(DirectoryManager directoryManager)
        {
            _directoryManager = directoryManager ?? throw new ArgumentNullException(nameof(directoryManager));
            _fileSystemBlobDescriptorStore = new FileSystemBlobDescriptorStore(_directoryManager);
        }

        public FileSystemBlobDescriptor Load(BlobId blobId)
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

        public void Save(FileSystemBlobDescriptor descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));

            var fileName = _directoryManager.GetDescriptorFileNameFromBlobId(descriptor.BlobId);
            File.WriteAllText(fileName, JsonConvert.SerializeObject(descriptor));
        }
    }
}
