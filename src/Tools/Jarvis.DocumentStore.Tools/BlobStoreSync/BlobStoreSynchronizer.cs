using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Jarvis.DocumentStore.Shell.BlobStoreSync
{
    public class BlobStoreSynchronizer
    {
        private readonly TenantConfiguration _config;

        internal BlobStoreSynchronizer(TenantConfiguration config, ILogger logger, Boolean startFromBeginning)
        {
            _config = config;
            _logger = logger;
            if (startFromBeginning)
            {
                ClearSyncInfo();
            }
        }

        private SyncInfo _info;

        private IBlobStore _originalStore;
        private IBlobStore _artifactStore;
        private IBlobStoreAdvanced _destinationOriginalStore;
        private IBlobStoreAdvanced _destinationArtifactsStoreAdvanced;
        private IMongoCollection<BsonDocument> _commitsCollection;

        private readonly ILogger _logger;

        internal void SetDirection(BlobStoreType source, BlobStoreType destination)
        {
            switch (source)
            {
                case BlobStoreType.GridFs:
                    _originalStore = _config.OriginalGridFsBlobStore;
                    _artifactStore = _config.ArtifactsGridFsBlobStore;
                    break;
                case BlobStoreType.FileSystem:
                    _originalStore = _config.OriginalFileSystemBlobStore;
                    _artifactStore = _config.ArtifactsFileSystemBlobStore;

                    break;
                default:
                    throw new NotSupportedException($"Source {source} not supported");
            }
            switch (destination)
            {
                case BlobStoreType.GridFs:
                    _destinationOriginalStore = _config.OriginalGridFsBlobStore;
                    _destinationArtifactsStoreAdvanced = _config.ArtifactsGridFsBlobStore;
                    break;
                case BlobStoreType.FileSystem:
                    _destinationOriginalStore = _config.OriginalFileSystemBlobStore;
                    _destinationArtifactsStoreAdvanced = _config.ArtifactsFileSystemBlobStore;
                    break;
                default:
                    throw new NotSupportedException($"Source {source} not supported");
            }
        }

        /// <summary>
        /// Perform a full sync from two different blob store.
        /// </summary>
        internal void PerformSync()
        {
            if (_info == null)
            {
                _info = GetInfo();
            }
            _logger.Info($"Start sync scanning from {_info.LastCommit}");

            // start reading the rmStream collection (maybe in chuncks)
            _commitsCollection = _config.EventStoreDb.GetCollection<BsonDocument>("Commits");
            SyncFiles();
            PersistInfo();
            _logger.Info("Syncrhonization complete.");
        }

        /// <summary>
        /// This function sync all blob from one storage to another one.
        /// </summary>
        /// <param name="commitsCollection">Collection of commits</param>
        private void SyncFiles()
        {
            List<BsonDocument> commits;
            do
            {
                commits = _commitsCollection
                   .Find(Builders<BsonDocument>.Filter.Gte("_id", _info.LastCommit))
                   .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
                   .Limit(100) //Avoid taking too many object, cursor may fail, the entire code become annoying
                   .ToList();
                foreach (var commit in commits)
                {
                    _logger.Debug($"Scanning commit {commit["_id"].AsInt64}");
                    BsonArray events = commit["Events"].AsBsonArray;
                    foreach (var evt in events)
                    {
                        var body = evt["Payload"]["Body"].AsBsonDocument;
                        var type = body["_t"].AsString;
                        if (type.StartsWith("DocumentDescriptorInitialized")
                            || type.StartsWith("FormatAddedToDocumentDescriptor"))
                        {
                            if (body.Names.Contains("BlobId"))
                            {
                                string blobId = body["BlobId"].AsString;
                                string descriptorId = commit["StreamId"].AsString;
                                Copy(new BlobId(blobId), descriptorId);
                            }
                        }
                        else if (type.StartsWith("DocumentDescriptorDeleted"))
                        {
                            if (body.Names.Contains("BlobId"))
                            {
                                string blobId = body["BlobId"].AsString;
                                Delete(new BlobId(blobId));
                            }
                        }
                    }

                    var checkpointToken = commit["_id"].AsInt64;
                    _info.LastCommit = checkpointToken + 1;
                    PersistInfo();
                }
            } while (commits.Count > 0); //loop until we have something to do.
        }

        private void Copy(BlobId blobId, String streamId)
        {
            IBlobStore source;
            IBlobStoreAdvanced destination;
            if (blobId.Format == "original")
            {
                source = _originalStore;
                destination = _destinationOriginalStore;
            }
            else
            {
                source = _artifactStore;
                destination = _destinationArtifactsStoreAdvanced;
            }
            IBlobDescriptor descriptor;

            if (BlobExists(destination, blobId))
            {
                _logger.DebugFormat("Blob {0} copy skipped beacuse already found on destination store", blobId);
                return;
            }

            try
            {
                descriptor = source.GetDescriptor(blobId);
            }
            catch (Exception ex)
            {
                var commits = _commitsCollection
                    .Find(Builders<BsonDocument>.Filter.Eq("StreamId", streamId))
                    .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
                    .ToEnumerable();
                var StreamDeleted = commits
                    .SelectMany(c => c["Events"].AsBsonArray.ToArray())
                    .Any(evt =>
                    {
                        var body = evt["Payload"]["Body"].AsBsonDocument;
                        var type = body["_t"].AsString;
                        return type.StartsWith("DocumentDescriptorDeleted");
                    });
                if (StreamDeleted)
                {
                    _logger.InfoFormat(ex, $"BlobId {blobId} not found on source stream, but the Document descriptor {streamId} was deleted!");
                }
                else
                {
                    _logger.ErrorFormat(ex, $"BlobId {blobId} not found on source stream");
                }
                return;
            }
            destination.RawStore(blobId, descriptor);
            _logger.Info($"Syncronized blob {blobId}");
        }

        private bool BlobExists(IBlobStoreAdvanced blobStoreAdvanced, BlobId blobId)
        {
            return blobStoreAdvanced.BlobExists(blobId);
        }

        private void Delete(BlobId blobId)
        {
            //Delete the blob if the blob is not present on the original store.
            //this never happens if you do a one-time migration, but if you do 
            //Continuous sync it could happen that the deleted blob was copied to
            //destination and should be removed.

            //Code is commented out, because deleted blob should remain for a certain amount
            //of time in recycle bin. It is better to create a cleanup job or a 
            //one time command that clean everything from the store. 

            //If we delete blob in destination blob store reacting to a DocumentDescriptorDeleted
            //event we risk not having recycle bin for the last deleted descriptor.
            //if (blobId.Format == "original")
            //{
            //    _destinationOriginalStore.Delete(blobId);
            //}
            //else
            //{
            //    _destinationArtifactsStoreAdvanced.Delete(blobId);
            //}
        }

        private class SyncInfo
        {
            public Int64 LastCommit { get; set; }
        }

        private void PersistInfo()
        {
            File.WriteAllText(GetInfoFileName(), _info.ToBsonDocument().ToString());
        }

        private SyncInfo GetInfo()
        {
            var fileName = GetInfoFileName();
            if (File.Exists(fileName))
            {
                var data = File.ReadAllText(fileName);
                return BsonSerializer.Deserialize<SyncInfo>(data);
            }
            return new SyncInfo();
        }

        private void ClearSyncInfo()
        {
            var fileName = GetInfoFileName();
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }

        private string GetInfoFileName()
        {
            return $".\\jobstatus.{_config.TenantId}.info";
        }
    }
}
