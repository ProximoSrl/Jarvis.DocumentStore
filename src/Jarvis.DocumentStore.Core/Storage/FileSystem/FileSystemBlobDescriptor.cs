using Jarvis.DocumentStore.Core.Model;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.IO;

namespace Jarvis.DocumentStore.Core.Storage
{
    /// <summary>
    /// This is the class that will be stored inside Mongodb to store additional
    /// information of the files.
    /// This is also the class that will be used to open a read stream.
    /// </summary>
    public class FileSystemBlobDescriptor : IBlobDescriptor
    {
        [BsonId]
        public BlobId BlobId { get; set; }

        public FileNameWithExtension FileNameWithExtension { get; set; }

        public Int64 Length { get; set; }

        public DateTime Timestamp { get; set; }

        public String Md5 { get; set; }

        public String ContentType { get; set; }

        public FileHash Hash
        {
            get { return new FileHash(Md5); }
        }

        public Boolean Exists => File.Exists(_localFileName);

        private String _localFileName;

        /// <summary>
        /// This class is persisted to MongoDb, but we do not want to hardcode the 
        /// full path of the file. (maybe we will want to move the file).
        /// This data is not persisted to mongo and was set
        /// by the storage before returning this value to the caller with the
        /// real path.
        /// </summary>
        /// <param name="localFileName"></param>
        internal void SetLocalFileName(String localFileName)
        {
            _localFileName = localFileName;
        }

        public Stream OpenRead()
        {
            if (String.IsNullOrEmpty(_localFileName))
                throw new Exception("Local file name was not correctly set by the blob store");
            return new FileStream(_localFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
    }
}
