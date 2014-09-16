using System.IO;
using MongoDB.Driver.GridFS;

namespace Jarvis.ImageService.Core.Storage
{
    public class GridFsFileStoreReader : IFileStoreReader
    {
        readonly MongoGridFSFileInfo _mongoGridFsFileInfo;

        public GridFsFileStoreReader(MongoGridFSFileInfo mongoGridFsFileInfo)
        {
            _mongoGridFsFileInfo = mongoGridFsFileInfo;
        }

        public Stream OpenRead()
        {
            return _mongoGridFsFileInfo.OpenRead();
        }

        public string FileName {
            get { return _mongoGridFsFileInfo.Metadata["originalFileName"].AsString;}
        }
    }
}