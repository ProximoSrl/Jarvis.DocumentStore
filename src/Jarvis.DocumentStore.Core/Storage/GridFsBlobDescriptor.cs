using System;
using System.IO;
using Jarvis.DocumentStore.Core.Model;
using MongoDB.Driver.GridFS;

namespace Jarvis.DocumentStore.Core.Storage
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
    }
}