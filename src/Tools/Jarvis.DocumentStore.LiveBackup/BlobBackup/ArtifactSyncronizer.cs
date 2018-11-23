
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jarvis.DocumentStore.LiveBackup.Support;

namespace Jarvis.DocumentStore.LiveBackup.BlobBackup
{
    /// <summary>
    /// Class used to syncronize the artifacts store. It performs a sync 
    /// from a source artifact store to a destination artifact store.
    /// </summary>
    internal class ArtifactSyncronizer
    {
        private readonly ArtifactSyncJobConfig _config;
        private readonly DirectoryBlobArchiveStore _store;

        internal ArtifactSyncronizer(
            ArtifactSyncJobConfig _config)
        {
            this._config = _config;
            _store = new DirectoryBlobArchiveStore(_config.Directory);
        }

        Info _info;

        /// <summary>
        /// Perform a full sync 
        /// </summary>
        internal void PerformSync()
        {
            if (_info == null)
            {
                _info = GetInfo();
            }
            // start reading the rmStream collection (maybe in chuncks)
            var sourceMongoUrl = new MongoUrl(_config.EvenstoreConnection);
            var sourceDatabase = new MongoClient(sourceMongoUrl).GetDatabase(sourceMongoUrl.DatabaseName);
            var rmStream = sourceDatabase.GetCollection<BsonDocument>("Commits");

            var blobsOriginalDatabaseMongoUrl = new MongoUrl(_config.OriginalBlobConnection);
            IMongoDatabase blobsOriginalDatabase = new MongoClient(blobsOriginalDatabaseMongoUrl).GetDatabase(blobsOriginalDatabaseMongoUrl.DatabaseName);

            var originalBlobBucket = new GridFSBucket<string>(blobsOriginalDatabase, new GridFSBucketOptions
            {
                BucketName = "original",
                ChunkSizeBytes = 1048576, // 1MB
            });

            SyncFiles(rmStream, originalBlobBucket);
            PersistInfo();
            Console.WriteLine("No more commit to sync!!");
            Thread.Sleep(2000);
        }

        /// <summary>
        /// Given the collection of stream of document store, it starts scanning stream collection 
        /// to undestand new blob that are added to the system and need to be copied.
        /// </summary>
        /// <param name="rmStream">Collection of the stream.</param>
        /// <param name="originalBlobBucket">Original bucket to read the blob.</param>
        private void SyncFiles(IMongoCollection<BsonDocument> rmStream, GridFSBucket<string> originalBlobBucket)
        {
            var files = rmStream
                .Find(Builders<BsonDocument>.Filter.Gte("_id", _info.LastCommit))
                .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
                .ToEnumerable();
            foreach (var file in files)
            {
                BsonArray events = file["Events"].AsBsonArray;
                foreach (var evt in events)
                {
                    var body = evt["Payload"]["Body"].AsBsonDocument;
                    var type = body["_t"].AsString;
                    if (type.StartsWith("DocumentDescriptorInitialized"))
                    {
                        if (body.Names.Contains("BlobId"))
                        {
                            string fileId = body["BlobId"].AsString;
                            if (fileId.StartsWith("original"))
                            {
                                UpdateDumpOfFile(fileId, originalBlobBucket, false);
                            }
                        }
                    }
                    else if (type.StartsWith("DocumentDescriptorDeleted"))
                    {
                        if (body.Names.Contains("BlobId"))
                        {
                            string fileId = body["BlobId"].AsString;
                            if (fileId.StartsWith("original"))
                            {
                                UpdateDumpOfFile(fileId, originalBlobBucket, true);
                            }
                        }
                    }
                }

                var checkpointToken = file["_id"].AsInt64;
                _info.LastCommit = checkpointToken + 1;
            }
        }

        /// <summary>
        /// Update dump of the file on disks.
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="originalBlobBucket"></param>
        /// <param name="deleted">If true this is an operation of deletion of the artifact.</param>
        /// <returns></returns>
        private Boolean UpdateDumpOfFile(
            string fileId,
            GridFSBucket<String> originalBlobBucket,
            Boolean deleted)
        {
            var findIdFilter = Builders<GridFSFileInfo<string>>.Filter.Eq(x => x.Id, fileId);

            var source = originalBlobBucket.Find(findIdFilter).FirstOrDefault();
            if (source == null)
            {
                return false; //source stream does not exists
            }
            if (!deleted)
            {
                using (var stream = originalBlobBucket.OpenDownloadStream(fileId))
                {
                    _store.Store(stream, source.Filename, fileId);
                }
            }
            else
            {
                _store.Delete(source.Filename, fileId);
            }

            return true;
        }

        private class Info
        {
            public Int64 LastCommit { get; set; }
        }

        private void PersistInfo()
        {
            File.WriteAllText(GetInfoFileName(), _info.ToBsonDocument().ToString());
        }

        private Info GetInfo()
        {
            var fileName = GetInfoFileName();
            if (File.Exists(fileName))
            {
                var data = File.ReadAllText(fileName);
                return BsonSerializer.Deserialize<Info>(data);
            }
            return new Info();
        }

        private string GetInfoFileName()
        {
            return _config.Directory + "\\jobstatus.info";
        }
    }
}
