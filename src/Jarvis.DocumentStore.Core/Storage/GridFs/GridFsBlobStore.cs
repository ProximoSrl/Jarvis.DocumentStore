using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.IdentitySupport;
using MongoDB.Bson;
using MongoDB.Driver;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using MongoDB.Driver.GridFS;

namespace Jarvis.DocumentStore.Core.Storage.GridFs
{
    public class GridFsBlobStore : IBlobStore
    {
        public ILogger Logger { get; set; }
        readonly IMongoDatabase _database;
        private readonly ICounterService _counterService;
        readonly ConcurrentDictionary<DocumentFormat, GridFsStorageInfo> _fs = new ConcurrentDictionary<DocumentFormat, GridFsStorageInfo>();

        public GridFsBlobStore(IMongoDatabase database, ICounterService counterService)
        {
            _database = database;
            _counterService = counterService;
            LoadFormatsFromDatabase();
        }

        private void LoadFormatsFromDatabase()
        {
            var cnames = _database.ListCollections();
            var names = cnames
                .ToEnumerable()
                .Select(d => d["name"].AsString)
                .Where(x => x.EndsWith(".files"))
                .Select(x => x.Substring(0, x.LastIndexOf(".files"))).ToArray();

            foreach (var name in names)
            {
                GetGridFsByFormat(new DocumentFormat(name));
            }
        }

        public IBlobWriter CreateNew(DocumentFormat format, FileNameWithExtension fname)
        {
            var blobId = new BlobId(format, _counterService.GetNext(format));
            var gridFs = GetGridFsByFormat(format);
            Logger.DebugFormat("Creating file {0} on {1}", blobId, gridFs.Database.DatabaseNamespace.DatabaseName);
            GridFSUploadOptions options = new GridFSUploadOptions();
            options.ContentType = MimeTypes.GetMimeType(fname);

            return new BlobWriter(blobId, gridFs.OpenUploadStream(blobId, fname, options), fname);
        }

        public IBlobDescriptor GetDescriptor(BlobId blobId)
        {
            var gridFs = GetGridFsByBlobId(blobId);

            Logger.DebugFormat("GetDescriptor for file {0} on {1}", blobId, gridFs.Database.DatabaseNamespace.DatabaseName);
            var findIdFilter = Builders<GridFSFileInfo<string>>.Filter.Eq(x => x.Id, blobId);
            var s = gridFs.Find(findIdFilter).SingleOrDefault();
            if (s == null)
            {
                var message = string.Format("Descriptor for file {0} not found!", blobId);
                Logger.DebugFormat(message);
                throw new Exception(message);
            }
            return new GridFsBlobDescriptor(blobId, s, gridFs);
        }

        public void Delete(BlobId blobId)
        {
            var gridFs = GetGridFsByBlobId(blobId);
            Logger.DebugFormat("Deleting file {0} on {1}", blobId, gridFs.Database.DatabaseNamespace.DatabaseName);
            gridFs.Delete((string)blobId);
        }

        public string Download(BlobId blobId, string folder)
        {
            var gridFs = GetGridFsByBlobId(blobId);

            Logger.DebugFormat("Downloading file {0} on {1} to folder {2}", blobId, gridFs.Database.DatabaseNamespace.DatabaseName, folder);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var findIdFilter = Builders<GridFSFileInfo<string>>.Filter.Eq(x => x.Id, blobId);
            var s = gridFs.Find(findIdFilter).Single();
            var localFileName = Path.Combine(folder, s.Filename);
            using (var fStream = new FileStream(localFileName, FileMode.Create, FileAccess.Write))
            {
                gridFs.DownloadToStream(blobId, fStream);
            }
            return localFileName;
        }

        public BlobId Upload(DocumentFormat format, string pathToFile)
        {
            using (var inStream = File.OpenRead(pathToFile))
            {
                return Upload(format, new FileNameWithExtension(Path.GetFileName(pathToFile)), inStream);
            }
        }

        public BlobId Upload(DocumentFormat format, FileNameWithExtension fileName, Stream sourceStream)
        {
            var gridFs = GetGridFsByFormat(format);
            using (var writer = CreateNew(format, fileName))
            {
                Logger.DebugFormat("Uploading file {0} named {1} on {2}", writer.BlobId, fileName, gridFs.Database.DatabaseNamespace.DatabaseName);
                sourceStream.CopyTo(writer.WriteStream);
                return writer.BlobId;
            }
        }

        public BlobStoreInfo GetInfo()
        {
            var aggregateDoc = BsonDocument.Parse("{$group:{_id:1, size:{$sum:'$length'}, count:{$sum:1}}}");

            var allInfos = _fs.Values
                .Select(x => x.FileInfoCollection
                    .Aggregate()
                    .AppendStage<BsonDocument>(aggregateDoc)
                    .FirstOrDefault())
                .Where(x => x != null)
                .Select(x => new { size = x["size"].ToInt64(), files = x["count"].ToInt64() })
                .ToArray();

            return new BlobStoreInfo(
                allInfos.Sum(x => x.size),
                allInfos.Sum(x => x.files)
            );
        }

        GridFSBucket<String> GetGridFsByFormat(DocumentFormat format)
        {
            return _fs.GetOrAdd(format, CreateGridFsForFormat).Bucket;
        }

        GridFsStorageInfo CreateGridFsForFormat(DocumentFormat format)
        {
            var bucket = new GridFSBucket<string>(_database, new GridFSBucketOptions
            {
                BucketName = format,
                ChunkSizeBytes = 1048576, // 1MB
            });

            var fileInfoCollection = _database.GetCollection<BsonDocument>($"{format}.files");
            return new GridFsStorageInfo(bucket, fileInfoCollection);
        }

        GridFSBucket<String> GetGridFsByBlobId(BlobId id)
        {
            return GetGridFsByFormat(id.Format);
        }

        private class GridFsStorageInfo
        {
            public GridFsStorageInfo(GridFSBucket<string> bucket, IMongoCollection<BsonDocument> fileInfoCollection)
            {
                Bucket = bucket;
                FileInfoCollection = fileInfoCollection;
            }

            public GridFSBucket<String> Bucket { get; private set; }

            public IMongoCollection<BsonDocument> FileInfoCollection { get; private set; }
        }
    }
}
