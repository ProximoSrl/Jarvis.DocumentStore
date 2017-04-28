using System;
using System.IO;
using Jarvis.DocumentStore.Core.Model;
using MongoDB.Driver.GridFS;

namespace Jarvis.DocumentStore.Core.Storage.GridFs
{
    public class GridFsBlobDescriptor : IBlobDescriptor
    {
        readonly MongoGridFSFileInfo _mongoGridFsFileInfo;

        public GridFsBlobDescriptor(BlobId blobId, MongoGridFSFileInfo mongoGridFsFileInfo)
        {
            if (mongoGridFsFileInfo == null) throw new ArgumentNullException("mongoGridFsFileInfo");
            _mongoGridFsFileInfo = mongoGridFsFileInfo;
            BlobId = blobId;

            FileNameWithExtension = new FileNameWithExtension(_mongoGridFsFileInfo.Name);
        }

        public BlobId BlobId { get; private set; }

        public Stream OpenRead()
        {
            return _mongoGridFsFileInfo.OpenRead();
        }

        public FileNameWithExtension FileNameWithExtension { get; private set; }

        public string ContentType
        {
            get { return _mongoGridFsFileInfo.ContentType; }
        }

        public FileHash Hash
        {
            get { return new FileHash(_mongoGridFsFileInfo.MD5); }
        }

        public long Length
        {
            get { return _mongoGridFsFileInfo.Length; }
        }

        /// <summary>
        /// When we are in Gridfs we assume that the content is there (noone should 
        /// have be tampered with database content).
        /// </summary>
        public bool Exists => true;
    }
}