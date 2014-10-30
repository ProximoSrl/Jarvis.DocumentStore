using System;
using System.IO;
using Jarvis.DocumentStore.Core.Model;
using MongoDB.Driver.GridFS;

namespace Jarvis.DocumentStore.Core.Storage
{
    public class GridFsFileStoreDescriptor : IFileStoreDescriptor
    {
        readonly MongoGridFSFileInfo _mongoGridFsFileInfo;

        public GridFsFileStoreDescriptor(FileId fileId, MongoGridFSFileInfo mongoGridFsFileInfo)
        {
            if (mongoGridFsFileInfo == null) throw new ArgumentNullException("mongoGridFsFileInfo");
            _mongoGridFsFileInfo = mongoGridFsFileInfo;
            FileId = fileId;

            FileNameWithExtension = new FileNameWithExtension(_mongoGridFsFileInfo.Name);
        }

        public FileId FileId { get; private set; }

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