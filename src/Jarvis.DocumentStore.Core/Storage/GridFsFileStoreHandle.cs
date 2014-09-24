using System;
using System.IO;
using MongoDB.Driver.GridFS;

namespace Jarvis.DocumentStore.Core.Storage
{
    public class GridFsFileStoreHandle : IFileStoreHandle
    {
        readonly MongoGridFSFileInfo _mongoGridFsFileInfo;

        public GridFsFileStoreHandle(MongoGridFSFileInfo mongoGridFsFileInfo)
        {
            if (mongoGridFsFileInfo == null) throw new ArgumentNullException("mongoGridFsFileInfo");
            _mongoGridFsFileInfo = mongoGridFsFileInfo;
        }

        public Stream OpenRead()
        {
            return _mongoGridFsFileInfo.OpenRead();
        }

        public string FileName {
            get { return _mongoGridFsFileInfo.Name;}
        }

        public string ContentType {
            get { return _mongoGridFsFileInfo.ContentType; }
        }
    }
}