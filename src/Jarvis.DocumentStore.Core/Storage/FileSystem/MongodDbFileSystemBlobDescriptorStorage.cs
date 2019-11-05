using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.Helpers;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;

namespace Jarvis.DocumentStore.Core.Storage.FileSystem
{
    public class MongodDbFileSystemBlobDescriptorStorage : IFileSystemBlobDescriptorStorage
    {
        private readonly IMongoCollection<FileSystemBlobDescriptor> _collection;

        public MongodDbFileSystemBlobDescriptorStorage(
            IMongoDatabase db,
            String collectionName)
        {
            _collection = db.GetCollection<FileSystemBlobDescriptor>(collectionName);
        }

        public FileSystemBlobDescriptor FindOneById(BlobId blobId)
        {
            return _collection.FindOneById(blobId);
        }

        public BlobStoreInfo GetStoreInfo()
        {
            var allInfos = _collection.Aggregate()
              .AppendStage<BsonDocument>(BsonDocument.Parse("{$group:{_id:1, size:{$sum:'$Length'}, count:{$sum:1}}}"))
              .ToEnumerable()
              .FirstOrDefault();
            if (allInfos == null)
                return new BlobStoreInfo(0, 0);

            return new BlobStoreInfo(allInfos["size"].AsInt64, allInfos["count"].AsInt32);
        }

        public void SaveDescriptor(FileSystemBlobDescriptor fileSystemBlobDescriptor)
        {
            _collection.Save(fileSystemBlobDescriptor, fileSystemBlobDescriptor.BlobId);
        }

        public void Delete(BlobId blobId)
        {
            _collection.RemoveById(blobId);
        }

        public override string ToString()
        {
            return $"MongoDbFileSystemDescriptor, pointing to db {_collection.Database.DatabaseNamespace.DatabaseName} collection {_collection.CollectionNamespace.CollectionName}";
        }
    }
}

