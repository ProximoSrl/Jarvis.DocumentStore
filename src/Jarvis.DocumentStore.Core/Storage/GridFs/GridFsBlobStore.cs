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
using MongoDB.Driver.GridFS;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;

namespace Jarvis.DocumentStore.Core.Storage.GridFs
{
    public class GridFsBlobStore : IBlobStoreAdvanced
    {
        public ILogger Logger { get; set; }

        private readonly MongoDatabase _database;
        private readonly ICounterService _counterService;
        private readonly ConcurrentDictionary<DocumentFormat, MongoGridFS> _fs = new ConcurrentDictionary<DocumentFormat, MongoGridFS>();

        public GridFsBlobStore(MongoDatabase database, ICounterService counterService)
        {
            _database = database;
            _counterService = counterService;
            LoadFormatsFromDatabase();
            Logger = NullLogger.Instance;
        }

        private void LoadFormatsFromDatabase()
        {
            var cnames = _database.GetCollectionNames().ToArray();
            var names = cnames
                .Where(x => x.EndsWith(".files"))
                .Select(x => x.Substring(0, x.LastIndexOf(".files")))
                .ToArray();

            foreach (var name in names)
            {
                GetGridFsByFormat(new DocumentFormat(name));
            }
        }

        public IBlobWriter CreateNew(DocumentFormat format, FileNameWithExtension fname)
        {
            var blobId = new BlobId(format, _counterService.GetNext(format));
            return CreateBlobWriterFromBlobId(format, fname, blobId);
        }

        private IBlobWriter CreateBlobWriterFromBlobId(DocumentFormat format, FileNameWithExtension fname, BlobId blobId)
        {
            var gridFs = GetGridFsByFormat(format);
            Logger.DebugFormat("Creating file {0} on {1}", blobId, gridFs.DatabaseName);
            var stream = gridFs.Create(fname, new MongoGridFSCreateOptions()
            {
                ContentType = MimeTypes.GetMimeType(fname),
                UploadDate = DateTime.UtcNow,
                Id = (string)blobId
            });

            return new BlobWriter(blobId, stream, fname);
        }

        public IBlobDescriptor GetDescriptor(BlobId blobId)
        {
            var gridFs = GetGridFsByBlobId(blobId);

            Logger.DebugFormat("GetDescriptor for blob {0} on gridfs {1}", blobId, gridFs.DatabaseName);
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

        public BlobId Upload(DocumentFormat format, FileNameWithExtension fileName, Stream sourceStream)
        {
            var gridFs = GetGridFsByFormat(format);
            using (var writer = CreateNew(format, fileName))
            {
                Logger.DebugFormat("Uploading file {0} named {1} on {2}", writer.BlobId, fileName, gridFs.DatabaseName);
                sourceStream.CopyTo(writer.WriteStream);
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
                allInfos.Sum(x => x.size),
                allInfos.Sum(x => x.files)
            );
        }

        private MongoGridFS GetGridFsByFormat(DocumentFormat format)
        {
            return _fs.GetOrAdd(format, CreateGridFsForFormat);
        }

        private MongoGridFS CreateGridFsForFormat(DocumentFormat format)
        {
            var settings = new MongoGridFSSettings()
            {
                Root = format
            };

            return _database.GetGridFS(settings);
        }

        private MongoGridFS GetGridFsByBlobId(BlobId id)
        {
            return GetGridFsByFormat(id.Format);
        }

        #region Advanced

        public bool BlobExists(BlobId blobId)
        {
            var gridFs = GetGridFsByBlobId(blobId);

            Logger.DebugFormat("BlobExists for blob {0} on gridfs {1}", blobId, gridFs.DatabaseName);
            var s = gridFs.FindOneById((string)blobId);
            return s != null;
        }

        public IBlobDescriptor Persist(BlobId blobId, string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return Persist(blobId, new FileNameWithExtension(Path.GetFileName(fileName)), fs);
            }
        }

        public IBlobDescriptor Persist(BlobId blobId, FileNameWithExtension fileName, Stream inputStream)
        {
            using (var writer = CreateBlobWriterFromBlobId(blobId.Format, fileName, blobId))
            {
                inputStream.CopyTo(writer.WriteStream);
            }
            return GetDescriptor(blobId);
        }

        /// <summary>
        /// We have no real Raw storage option for Gridfs, but we can simply store all raw bytes
        /// inside the gridfs.
        /// </summary>
        /// <param name="blobId"></param>
        /// <param name="descriptor"></param>
        public void RawStore(BlobId blobId, IBlobDescriptor descriptor)
        {
            using (var writer = CreateBlobWriterFromBlobId(blobId.Format, descriptor.FileNameWithExtension, blobId))
            using (var inputStream = descriptor.OpenRead())
            {
                inputStream.CopyTo(writer.WriteStream);
            }
        }

        #endregion
    }
}
