using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Castle.Core.Logging;
using CQRS.Shared.IdentitySupport;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Jarvis.DocumentStore.Core.Storage
{
    public class GridFsBlobStore : IBlobStore
    {
        public ILogger Logger { get; set; }
        readonly MongoDatabase _database;
        private readonly ICounterService _counterService;
        readonly ConcurrentDictionary<DocumentFormat, MongoGridFS> _fs = new ConcurrentDictionary<DocumentFormat, MongoGridFS>();

        public GridFsBlobStore(MongoDatabase database, ICounterService counterService)
        {
            _database = database;
            _counterService = counterService;
        }

        public IBlobWriter CreateNew(DocumentFormat format, FileNameWithExtension fname)
        {
            var blobId = new BlobId(format, _counterService.GetNext(format));
            var gridFs = GetGridFsByFormat(format);
            Logger.DebugFormat("Creating file {0} on {1}", blobId, gridFs.DatabaseName);
            var stream = gridFs.Create(fname, new MongoGridFSCreateOptions()
            {
                ContentType = MimeTypes.GetMimeType(fname),
                UploadDate = DateTime.UtcNow,
                Id = (string)blobId
            });

            return new BlobWriter(blobId, stream,fname);
        }

        public Stream CreateNew(BlobId blobId, FileNameWithExtension fname)
        {
            var gridFs = GetGridFsByBlobId(blobId);

            Logger.DebugFormat("Creating file {0} on {1}", blobId, gridFs.DatabaseName);
            Delete(blobId);
            return gridFs.Create(fname, new MongoGridFSCreateOptions()
            {
                ContentType = MimeTypes.GetMimeType(fname),
                UploadDate = DateTime.UtcNow,
                Id = (string)blobId
            });
        }

        public IBlobDescriptor GetDescriptor(BlobId blobId)
        {
            var gridFs = GetGridFsByBlobId(blobId);

            Logger.DebugFormat("GetDescriptor for file {0} on {1}", blobId, gridFs.DatabaseName);
            var s = gridFs.FindOneById((string)blobId);
            if (s == null)
            {
                var message = string.Format("Descriptor for file {0} not found!", blobId);
                Logger.DebugFormat(message);
                throw new Exception(message);
            }
            return new GridFsBlobDescriptor(blobId, s);
        }

        public void Delete(BlobId blobId)
        {
            var gridFs = GetGridFsByBlobId(blobId);
            Logger.DebugFormat("Deleting file {0} on {1}", blobId, gridFs.DatabaseName);
            gridFs.DeleteById((string)blobId);
        }

        public string Download(BlobId blobId, string folder)
        {
            var gridFs = GetGridFsByBlobId(blobId);

            Logger.DebugFormat("Downloading file {0} on {1} to folder {2}", blobId, gridFs.DatabaseName, folder);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var s = gridFs.FindOneById((string)blobId);
            var localFileName = Path.Combine(folder, s.Name);
            gridFs.Download(localFileName, s);
            return localFileName;
        }

        public BlobId Upload(DocumentFormat format, string pathToFile)
        {
            using (var inStream = File.OpenRead(pathToFile))
            {
                return Upload(format, new FileNameWithExtension(Path.GetFileName(pathToFile)), inStream);
            }
        }

        public BlobId Upload(DocumentFormat format, FileNameWithExtension fileName, Stream sourceStrem)
        {
            var gridFs = GetGridFsByFormat(format);
            using (var writer = CreateNew(format, fileName))
            {
                Logger.DebugFormat("Uploading file {0} named {1} on {2}", writer.BlobId, fileName, gridFs.DatabaseName);
                sourceStrem.CopyTo(writer.WriteStream);
                return writer.BlobId;
            }
        }

        public BlobStoreInfo GetInfo()
        {
            var aggregation = new AggregateArgs()
            {
                Pipeline = new[] { BsonDocument.Parse("{$group:{_id:1, size:{$sum:'$length'}, count:{$sum:1}}}") }
            };

            var allInfos = _fs.Values
                .Select(x => x.Files.Aggregate(aggregation).FirstOrDefault())
                .Where(x => x != null)
                .Select(x => new { size = x["size"].ToInt64(), files = x["count"].ToInt64() })
                .ToArray();

            return new BlobStoreInfo(
                allInfos.Sum(x=>x.size),
                allInfos.Sum(x=>x.files)
            );
        }

        MongoGridFS GetGridFsByFormat(DocumentFormat format)
        {
            return _fs.GetOrAdd(format, CreateGridFsForFormat);
        }

        MongoGridFS CreateGridFsForFormat(DocumentFormat format)
        {
            var settings = new MongoGridFSSettings()
            {
                Root = format
            };

            return _database.GetGridFS(settings);
        }

        MongoGridFS GetGridFsByBlobId(BlobId id)
        {
            return GetGridFsByFormat(id.Format);
        }
    }
}
