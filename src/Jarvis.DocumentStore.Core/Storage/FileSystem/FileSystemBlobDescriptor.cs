using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage.FileSystem;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.IO;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;

namespace Jarvis.DocumentStore.Core.Storage
{
    /// <summary>
    /// This is the class that will be stored inside Mongodb to store additional
    /// information of the files.
    /// This is also the class that will be used to open a read stream.
    /// </summary>
    public class FileSystemBlobDescriptor : IBlobDescriptor
    {
        internal FileSystemBlobDescriptor(
            DirectoryManager manager,
            BlobId blobId,
            FileNameWithExtension fileNameWithExtension,
            DateTime timeStamp,
            String contentType)
        {
            BlobId = blobId;
            FileNameWithExtension = fileNameWithExtension;
            Timestamp = timeStamp;
            ContentType = contentType;
            LocalFileName = manager.GetFileNameFromBlobId(BlobId, FileNameWithExtension);
        }

        private FileSystemBlobDescriptor()
        {
        }

        public static FileSystemBlobDescriptor CreateForReference(
            BlobId blobId,
            string filePath,
            DateTime timeStamp,
            String contentType)
        {
            return new FileSystemBlobDescriptor()
            {
                BlobId = blobId,
                FileNameWithExtension = new FileNameWithExtension(filePath),
                Timestamp = timeStamp,
                ContentType = contentType,
                IsReference = true,
                LocalFileName = filePath, //Storage of the file is the original place, do not copy
                OriginalReferenceFilePath = filePath //to simplify troubleshooting we save the original file path, useful if we decide to move the file to internal storage
            };
        }

        [BsonId]
        public BlobId BlobId { get; private set; }

        public FileNameWithExtension FileNameWithExtension { get; private set; }

        public Int64 Length { get; set; }

        public DateTime Timestamp { get; private set; }

        public String Md5 { get; set; }

        public String ContentType { get; private set; }

        /// <summary>
        /// If true this descriptor is a reference and it does not own the
        /// content of the file.
        /// </summary>
        public bool IsReference { get; private set; }

        /// <summary>
        /// This property contains the original name of the file if the
        /// descriptor was created as a reference. This information was
        /// a duplicate of <see cref="LocalFileName"/> but if the file was
        /// changed from a reference to a standard file, it is nice to maintain
        /// the original location as a reference for future inspection.
        /// </summary>
        public string OriginalReferenceFilePath { get; private set; }

        public FileHash Hash
        {
            get { return new FileHash(Md5); }
        }

        public Boolean Exists => File.Exists(LocalFileName);

        /// <summary>
        /// This contains the full path of the file in the disk. It usually not null
        /// except for old version of the file system blob store that does not persist
        /// full local file name information.
        /// </summary>
        public String LocalFileName { get; private set; }

        public Stream OpenRead()
        {
            if (String.IsNullOrEmpty(LocalFileName))
            {
                throw new Exception("Local file name was not correctly set by the blob store");
            }

            return File.Open(LocalFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        /// <summary>
        /// <para>
        /// Since in the first version of the descriptor full name is not saved, and LocalFileName
        /// property was not set, we needs to maintain the old method that calculate the physical location
        /// of the file from the logical file name.
        /// </para>
        /// <para>
        /// This method of work was used to allow transfer for file, we save in descriptor only file name,
        /// but the relative location was specified at runtime. This will allow for moving data into another
        /// path without changing descriptor
        /// </para>
        /// <para>
        /// We decided to store fullname to allow the usage of more that one single directory, we could use
        /// more than one directory manager, and change the place where the file is stored. This is useful if
        /// storage become full and we can simply add another storage. (this functionality is still unsupported
        /// but storing fullname into the descriptor is a pre-requisite)
        /// </para>
        /// </summary>
        /// <param name="directoryManager"></param>
        internal void SetLocalFileName(DirectoryManager directoryManager)
        {
            if (!IsReference)
            {
                //LocalFileName could be null (first version of file system blob storage) so we get the
                //full name using the directory manager. 
                if (string.IsNullOrEmpty(LocalFileName))
                {
                    LocalFileName = directoryManager.GetFileNameFromBlobId(BlobId, FileNameWithExtension);
                }
                else
                {
                    //Local file name was already saved, this means that this record was saved with the second
                    //version, when fullname was specifyed on record.
                }
            }
        }
    }
}
