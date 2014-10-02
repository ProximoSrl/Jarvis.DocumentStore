using System;
using System.IO;
using Jarvis.DocumentStore.Core.Model;
using MongoDB.Driver.GridFS;

namespace Jarvis.DocumentStore.Core.Storage
{
    public class GridFsFileDescriptor : IFileDescriptor
    {
        readonly MongoGridFSFileInfo _mongoGridFsFileInfo;

        public GridFsFileDescriptor(MongoGridFSFileInfo mongoGridFsFileInfo)
        {
            if (mongoGridFsFileInfo == null) throw new ArgumentNullException("mongoGridFsFileInfo");
            _mongoGridFsFileInfo = mongoGridFsFileInfo;
        }

        public Stream OpenRead()
        {
            return _mongoGridFsFileInfo.OpenRead();
        }

        public string FileName
        {
            get { return _mongoGridFsFileInfo.Name; }
        }

        public string FileExtension
        {
            get { return Path.GetExtension(FileName).ToLowerInvariant(); }
        }

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