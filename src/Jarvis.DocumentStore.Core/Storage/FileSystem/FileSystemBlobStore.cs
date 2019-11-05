using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage.FileSystem;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.Framework.Shared.IdentitySupport;
using MongoDB.Driver;
using System;
using System.IO;
using System.Security.Cryptography;
using Directory = Jarvis.DocumentStore.Shared.Helpers.DsDirectory;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;

namespace Jarvis.DocumentStore.Core.Storage
{
    /// <summary>
    /// Class that implements a file system that stores binary blob on disk and
    /// additional informations in a standard mongo collection. This is needed to 
    /// avoid having GB of data inside of a GridFs, and keeping in mongo only
    /// the information that are necessary, leaving the binary blob to file system.
    /// </summary>
    public class FileSystemBlobStore : IBlobStoreAdvanced
    {
        public const string OriginalDescriptorStorageCollectionName = "originals.descriptor";
        public const string ArtifactsDescriptorStorageCollectionName = "artifacts.descriptor";

        public ILogger Logger { get; set; } = NullLogger.Instance;

        private const int FolderPrefixLength = 3;

        //private readonly IFileSystemBlobDescriptorStorage _fileSystemBlobDescriptorStore;
        private readonly IFileSystemBlobDescriptorStorage _mongodDbFileSystemBlobDescriptorStorage;
        private readonly ICounterService _counterService;

        private readonly DirectoryManager _directoryManager;

        /// <summary>
        /// Standard Constructor
        /// </summary>
        /// <param name="db">The database where I want to store information</param>
        /// <param name="collectionName">The name of the collection that will be used
        /// to store information of the file</param>
        /// <param name="baseDirectory">Base directory on filesystem where binary blob
        /// will be stored</param>
        /// <param name="counterService">Counter service to generate new <see cref="BlobId"/></param>
        /// <param name="documentStoreConfiguration">Global configuration to grab storage user name and password
        /// </param>
        public FileSystemBlobStore(
            IMongoDatabase db,
            String collectionName,
            String baseDirectory,
            ICounterService counterService,
            DocumentStoreConfiguration documentStoreConfiguration) : this(
                db,
                collectionName,
                baseDirectory,
                counterService,
                documentStoreConfiguration.StorageUserName,
                documentStoreConfiguration.StoragePassword)
        {
        }

        /// <summary>
        /// Standard Constructor
        /// </summary>
        /// <param name="db">The database where this component will save information
        /// for file descriptor. It is needed to perform queries.</param>
        /// <param name="collectionName">The name of the collection that will be used
        /// to store information of the file</param>
        /// <param name="baseDirectory">Base directory on filesystem where binary blob
        /// will be stored</param>
        /// <param name="counterService">Counter service to generate new <see cref="BlobId"/></param>
        public FileSystemBlobStore(
            IMongoDatabase db,
            String collectionName,
            String baseDirectory,
            ICounterService counterService,
            String userName,
            String password)
        {
            _directoryManager = new DirectoryManager(baseDirectory, FolderPrefixLength);
            _counterService = counterService;

            //_fileSystemBlobDescriptorStore = new FileSystemBlobDescriptorStore(_directoryManager);
            _mongodDbFileSystemBlobDescriptorStorage = new MongodDbFileSystemBlobDescriptorStorage(db, collectionName);
            _counterService = counterService;
            if (!String.IsNullOrEmpty(userName))
            {
                PinvokeWindowsNetworking.ConnectToRemote(baseDirectory, userName, password);
            }
        }

        public IBlobWriter CreateNew(DocumentFormat format, FileNameWithExtension fname)
        {
            var blobId = new BlobId(format, _counterService.GetNext(format));
            if (Logger.IsDebugEnabled) Logger.Debug($"CreateNew blob for format {format} with file {fname} - assigned blobId: {blobId}");
            return new FileSystemBlobWriter(
                blobId,
                fname,
                GetFileNameFromBlobIdAndRemoveDuplicates(blobId, fname),
                _mongodDbFileSystemBlobDescriptorStorage,
                Logger);
        }

        public IBlobDescriptor GetDescriptor(BlobId blobId)
        {
            if (blobId == null)
                throw new ArgumentNullException(nameof(blobId));

            Logger.DebugFormat($"GetDescriptor for blobid {blobId}");

            var descriptor = _mongodDbFileSystemBlobDescriptorStorage.FindOneById(blobId);
            if (descriptor == null)
            {
                var message = $"Descriptor for blobid {blobId} not found in database {_mongodDbFileSystemBlobDescriptorStorage.ToString()}!";
                Logger.Info(message);
                throw new Exception(message);
            }
            descriptor.SetLocalFileName(_directoryManager.GetFileNameFromBlobId(blobId, descriptor.FileNameWithExtension));
            return descriptor;
        }

        public bool BlobExists(BlobId blobId)
        {
            if (blobId == null)
                throw new ArgumentNullException(nameof(blobId));

            Logger.DebugFormat($"BlobExists for blobid {blobId}");

            var descriptor = _mongodDbFileSystemBlobDescriptorStorage.FindOneById(blobId);
            return descriptor != null;
        }

        public BlobStoreInfo GetInfo()
        {
            return _mongodDbFileSystemBlobDescriptorStorage.GetStoreInfo();
        }

        public BlobId Upload(DocumentFormat format, string pathToFile)
        {
            FileInfo finfo = new FileInfo(pathToFile);
            if (!finfo.Exists)
                throw new ArgumentException($"File {pathToFile} not found");

            if (Logger.IsDebugEnabled) Logger.Debug($"Upload document format {format}. File: {pathToFile}");
            using (var fileStream = new FileStream(pathToFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var descriptor = SaveStream(format, new FileNameWithExtension(Path.GetFileName(pathToFile)), fileStream);
                if (Logger.IsDebugEnabled) Logger.Debug($"Uploaded document format {format}. File: {pathToFile} with blob Id {descriptor.BlobId}");
                _mongodDbFileSystemBlobDescriptorStorage.SaveDescriptor(descriptor);
                return descriptor.BlobId;
            }
        }

        public BlobId Upload(DocumentFormat format, FileNameWithExtension fileName, Stream sourceStream)
        {
            var descriptor = SaveStream(format, fileName, sourceStream);
            if (Logger.IsDebugEnabled) Logger.Debug($"Uploaded document format {format} from stream with filename {fileName} with blob Id {descriptor.BlobId}");
            _mongodDbFileSystemBlobDescriptorStorage.SaveDescriptor(descriptor);

            return descriptor.BlobId;
        }

        /// <summary>
        /// This is the real function that save a stream to the destination
        /// file and create the descriptor
        /// </summary>
        /// <param name="format"></param>
        /// <param name="fileName"></param>
        /// <param name="sourceStream"></param>
        /// <returns></returns>
        private FileSystemBlobDescriptor SaveStream(DocumentFormat format, FileNameWithExtension fileName, Stream sourceStream)
        {
            var blobId = new BlobId(format, _counterService.GetNext(format));
            return InnerPersistOfBlob(blobId, fileName, sourceStream);
        }

        /// <summary>
        /// Persist a stream given a blobId.
        /// </summary>
        /// <param name="blobId"></param>
        /// <param name="fileName"></param>
        /// <param name="sourceStream"></param>
        /// <returns></returns>
        private FileSystemBlobDescriptor InnerPersistOfBlob(BlobId blobId, FileNameWithExtension fileName, Stream sourceStream)
        {
            FileSystemBlobDescriptor descriptor = new FileSystemBlobDescriptor()
            {
                BlobId = blobId,
                FileNameWithExtension = fileName,
                Timestamp = DateTime.Now,
                ContentType = MimeTypes.GetMimeType(fileName)
            };
            string destinationFileName = GetFileNameFromBlobIdAndRemoveDuplicates(blobId, descriptor.FileNameWithExtension);
            Logger.Debug($"File {fileName} was assigned blob {blobId} and will be saved in file {destinationFileName}");

            using (var md5 = MD5.Create())
            using (var fileStream = new FileStream(destinationFileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fileStream.Seek(0, SeekOrigin.Begin);
                fileStream.SetLength(0);

                Byte[] buffer = new Byte[8192];
                Int32 read;
                Int64 length = 0;
                while ((read = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    md5.TransformBlock(buffer, 0, read, buffer, 0);
                    fileStream.Write(buffer, 0, read);
                    length += read;
                }
                md5.TransformFinalBlock(buffer, 0, 0);
                descriptor.Length = length;
                descriptor.Md5 = BitConverter.ToString(md5.Hash).Replace("-", "");
            }
            Logger.Info($"Blob {blobId} saved in file {destinationFileName} with hash {descriptor.Md5} and length {descriptor.Length}");
            return descriptor;
        }

        private string GetFileNameFromBlobIdAndRemoveDuplicates(BlobId blobId, String fileName)
        {
            var finalFileName = _directoryManager.GetFileNameFromBlobId(blobId, fileName);
            if (File.Exists(finalFileName))
            {
                //Anomaly, we are trying to overwrite the blob
                Logger.Warn($"Destination file {blobId} already exists for id {blobId}");

                try
                {
                    //Todo move in another folder ... maybe a lost and found.
                    File.Move(finalFileName, finalFileName + "." + Guid.NewGuid().ToString());
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error moving file {finalFileName} with guid suffix", ex);
                }
            }

            return finalFileName;
        }

        public string Download(BlobId blobId, string folder)
        {
            if (blobId == null)
                throw new ArgumentNullException(nameof(blobId));

            if (String.IsNullOrEmpty(folder))
                throw new ArgumentNullException(nameof(folder));

            if (!Directory.Exists(folder))
                throw new ArgumentException($"folder {folder} does not exists", nameof(folder));

            var descriptor = _mongodDbFileSystemBlobDescriptorStorage.FindOneById(blobId);
            if (descriptor == null)
                throw new ArgumentException($"Descriptor for {blobId} not found in {_mongodDbFileSystemBlobDescriptorStorage.GetType().Name}");

            var localFileName = _directoryManager.GetFileNameFromBlobId(blobId, descriptor.FileNameWithExtension);
            if (!File.Exists(localFileName))
            {
                Logger.Error($"Blob {blobId} has descriptor, but blob file {localFileName} not found in the system.");
                throw new ArgumentException($"Blob {blobId} not found");
            }

            var originalFileName = descriptor.FileNameWithExtension.ToString();
            string destinationFileName = Path.Combine(folder, originalFileName);
            Int32 uniqueId = 1;
            while (File.Exists(destinationFileName))
            {
                destinationFileName = Path.Combine(folder, Path.GetFileNameWithoutExtension(originalFileName) + $" ({uniqueId++})") + Path.GetExtension(originalFileName);
            }

            File.Copy(localFileName, destinationFileName);

            if (Logger.IsDebugEnabled) Logger.Debug($"Blob {blobId} downloaded in folder {folder} with name {destinationFileName}");
            return destinationFileName;
        }

        public void Delete(BlobId blobId)
        {
            var descriptor = _mongodDbFileSystemBlobDescriptorStorage.FindOneById(blobId);
            if (descriptor == null)
                throw new ArgumentException($"Descriptor for {blobId} not found in {_mongodDbFileSystemBlobDescriptorStorage.GetType().Name}");

            var fileName = _directoryManager.GetFileNameFromBlobId(blobId, descriptor.FileNameWithExtension);
            File.Delete(fileName);

            _mongodDbFileSystemBlobDescriptorStorage.Delete(blobId);
        }

        #region Advanced

        public IBlobDescriptor Persist(BlobId blobId, string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return Persist(blobId, new FileNameWithExtension(Path.GetFileName(fileName)), fs);
            }
        }

        public IBlobDescriptor Persist(BlobId blobId, FileNameWithExtension fileName, Stream inputStream)
        {
            var descriptor = InnerPersistOfBlob(blobId, fileName, inputStream);
            _mongodDbFileSystemBlobDescriptorStorage.SaveDescriptor(descriptor);
            return descriptor;
        }

        public void RawStore(BlobId blobId, IBlobDescriptor descriptor)
        {
            Logger.InfoFormat("Ask for raw storage of blob {0} for file {1}", blobId, descriptor.FileNameWithExtension);
            using (var stream = descriptor.OpenRead())
            {
                InnerPersistOfBlob(blobId, descriptor.FileNameWithExtension, stream);
                FileSystemBlobDescriptor newDescriptor = new FileSystemBlobDescriptor()
                {
                    BlobId = blobId,
                    ContentType = descriptor.ContentType,
                    FileNameWithExtension = descriptor.FileNameWithExtension,
                    Length = descriptor.Length,
                    Md5 = descriptor.Hash.ToString(),
                    Timestamp = DateTime.UtcNow,
                };
                _mongodDbFileSystemBlobDescriptorStorage.SaveDescriptor(newDescriptor);
            }
        }

        #endregion
    }
}
