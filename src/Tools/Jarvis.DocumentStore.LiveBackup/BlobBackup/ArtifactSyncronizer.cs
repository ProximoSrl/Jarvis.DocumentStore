
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Jarvis.DocumentStore.LiveBackup.BlobBackup
{
    /// <summary>
    /// Class used to syncronize the artifacts store. It performs a sync 
    /// from a <see cref="IBlobStore" /> to an <see cref="IBlobArchiveStore"/>
    /// </summary>
    internal class ArtifactSyncronizer
    {
        private readonly ArtifactSyncJobConfig _config;
        private readonly DirectoryBlobArchiveStore _store;

        public ILogger Logger { get; set; }

        internal ArtifactSyncronizer(
            ArtifactSyncJobConfig _config,
            ILogger logger)
        {
            this._config = _config;
            _store = new DirectoryBlobArchiveStore(_config.Directory);
            Logger = logger;
        }

        /// <summary>
        /// This is the information on the last checkpoint backupped 
        /// </summary>
        private Info _info;

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
            var commitsCollection = sourceDatabase.GetCollection<BsonDocument>("Commits");

            SyncFiles(commitsCollection, _config.OriginalBlobConnection);
            PersistInfo();
            Console.WriteLine("No more commit to sync!!");
            Thread.Sleep(2000);
        }

        /// <summary>
        /// Given the collection of stream of document store, it starts scanning stream collection 
        /// to undestand new blob that are added to the system and need to be copied.
        /// </summary>
        /// <param name="commitsCollection">Collection of all the commits of documenstore</param>
        /// <param name="blobStore">Blob store that points to store for original blob.</param>
        private void SyncFiles(IMongoCollection<BsonDocument> commitsCollection, IBlobStore blobStore)
        {
            var commits = commitsCollection
                .Find(Builders<BsonDocument>.Filter.Gte("_id", _info.LastCommit))
                .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
                .ToEnumerable();
            foreach (var commit in commits)
            {
                BsonArray events = commit["Events"].AsBsonArray;
                var checkpointToken = commit["_id"].AsInt64;
                foreach (var evt in events)
                {
                    var body = evt["Payload"]["Body"].AsBsonDocument;
                    var type = body["_t"].AsString;
                    if (type.StartsWith("DocumentDescriptorInitialized"))
                    {
                        ScanCommit(blobStore, checkpointToken, body, false);
                    }
                    else if (type.StartsWith("DocumentDescriptorDeleted"))
                    {
                        ScanCommit(blobStore, checkpointToken, body, true);
                    }
                }

                _info.LastCommit = checkpointToken + 1;
            }
        }

        /// <summary>
        /// Scan a BsonDocument looking for the <see cref="BlobId"/> of the blob and if a <see cref="BlobId" />
        /// is found it update the dump of file to disk.
        /// </summary>
        /// <param name="originalBlobStore"></param>
        /// <param name="body"></param>
        /// <param name="deleted"></param>
        private void ScanCommit(IBlobStore originalBlobStore, Int64 commitId, BsonDocument body, Boolean deleted)
        {
            if (Logger.IsDebugEnabled)
                Logger.Debug($"Scanning commit {commitId} for event {body["_t"].AsString}");

            if (body.Names.Contains("BlobId"))
            {
                string blobId = body["BlobId"].AsString;
                if (blobId.StartsWith("original"))
                {
                    Logger.Info($"Copy blob {blobId} to fileStore");
                    UpdateDumpOfFile(blobId, originalBlobStore, deleted);
                }
            }
        }

        /// <summary>
        /// Update dump of the file on disks.
        /// </summary>
        /// <param name="blobId"></param>
        /// <param name="originalBlobStore"></param>
        /// <param name="deleted">If true this is an operation of deletion of the artifact.</param>
        /// <returns></returns>
        private Boolean UpdateDumpOfFile(
            string blobId,
            IBlobStore originalBlobStore,
            Boolean deleted)
        {
            try
            {
                //Take descriptor even if the blob is deleted, because deleting a descriptor leav
                //blob in recycle bin, this imply that the blob should still be there
                var descriptor = originalBlobStore.GetDescriptor(new BlobId(blobId));
                if (!deleted)
                {
                    using (var stream = descriptor.OpenRead())
                    {
                        _store.Store(stream, descriptor.FileNameWithExtension, blobId);
                    }
                    Logger.Debug($"Blob {blobId} copied to backup store. FileName: {descriptor.FileNameWithExtension}");
                }
                else
                {
                    _store.Delete(descriptor.FileNameWithExtension, blobId);
                    Logger.Debug($"Blob {blobId} was deleted, delete from the backup store. FileName: {descriptor.FileNameWithExtension}");
                }
                return true;
            }
            catch (Exception ex)
            {
                if (deleted == false)
                {
                    Logger.Error($"Unable to backup blob {blobId} from original store. {ex.Message}", ex);
                }
                else
                {
                    //Since the blob is deleted, issue a warning, because it could be that the backup started
                    //late and the blob was already purged from recycle bin.
                    Logger.Warn($"Unable to backup blob {blobId} from original store. {ex.Message}. Maybe the blob was already deleted by job.", ex);
                }
                return false;
            }
        }

        private class Info
        {
            public Int64 LastCommit { get; set; }
        }

        /// <summary>
        /// Persist sync info to a file. This allow the backup directory to have both 
        /// backup and the file that stores the last status of the backup.
        /// </summary>
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
