using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fasterflect;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Jarvis.ImageService.Core.Storage
{
    public class FileStore : IFileStore
    {
        readonly MongoGridFS _gridFs;
        public FileStore(MongoDatabase db)
        {
            _gridFs = db.GetGridFS(MongoGridFSSettings.Defaults);
        }

        public Stream CreateNew(string fileId, string fname)
        {
            var metadata = new BsonDocument
            {
                { "originalFileName", fname}
            };

            _gridFs.DeleteById(fileId);

            return _gridFs.Create(fname, new MongoGridFSCreateOptions()
            {
                ContentType = MimeTypes.GetMimeType(fname),
                UploadDate = DateTime.UtcNow,
                Metadata = metadata,
                Id = fileId
            });
        }

        public IFileStoreReader OpenRead(string fileId)
        {
            var s = _gridFs.FindOneById(fileId);
            return new GridFsFileStoreReader(s);
        }
    }
}
