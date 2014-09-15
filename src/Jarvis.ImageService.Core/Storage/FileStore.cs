using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Jarvis.ImageService.Core.Storage
{
    public interface IFileStore
    {
        Stream CreateNew(string fname, string rootName);
    }

    public class FileStore : IFileStore
    {
        readonly MongoGridFS _gridFs;
        public FileStore(MongoDatabase db)
        {
            _gridFs = db.GetGridFS(MongoGridFSSettings.Defaults);
        }

        public Stream CreateNew(string fname, string rootName)
        {
            var metadata = new BsonDocument
            {
                { "originalFileName", fname}
            };

            var uniqueFileName = rootName + "/source";

            return _gridFs.Create(uniqueFileName, new MongoGridFSCreateOptions()
            {
                ContentType = MimeTypes.GetMimeType(fname),
                UploadDate = DateTime.UtcNow,
                Metadata = metadata,
                Aliases = new []{ fname, rootName},
                Id = uniqueFileName
            });
        }
    }
}
