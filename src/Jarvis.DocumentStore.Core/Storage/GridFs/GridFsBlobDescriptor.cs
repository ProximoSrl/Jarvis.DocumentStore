using System;
using System.IO;
using Jarvis.DocumentStore.Core.Model;
using MongoDB.Driver.GridFS;

namespace Jarvis.DocumentStore.Core.Storage.GridFs
{
    public class GridFsBlobDescriptor : IBlobDescriptor
    {
        private readonly GridFSFileInfo<String> _mongoGridFsFileInfo;
        private readonly GridFSBucket<String> _bucket;

        public GridFsBlobDescriptor(BlobId blobId, GridFSFileInfo<String> mongoGridFsFileInfo, GridFSBucket<String> bucket)
        {
            if (mongoGridFsFileInfo == null) throw new ArgumentNullException("mongoGridFsFileInfo");
            _mongoGridFsFileInfo = mongoGridFsFileInfo;
            BlobId = blobId;

            FileNameWithExtension = new FileNameWithExtension(_mongoGridFsFileInfo.Filename);
            _bucket = bucket;
        }

        public BlobId BlobId { get; private set; }

        public Stream OpenRead()
        {
            return _bucket.OpenDownloadStream(BlobId);
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