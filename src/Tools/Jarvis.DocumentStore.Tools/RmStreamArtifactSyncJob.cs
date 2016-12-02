using Jarvis.ConfigurationService.Client;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Shared.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Tools
{
    public class ArtifactSyncJobConfig
    {
        public ArtifactSyncJobConfig(String tenantId)
        {
            Tenant = tenantId;
            EventStoreConnectionString = ConfigurationServiceClient.Instance.GetSetting("connectionStrings." + tenantId + ".events");
            SourceOriginalConnectionString = ConfigurationServiceClient.Instance.GetSetting("connectionStrings." + tenantId + ".originals");
            SourceArtifactsConnectionString = ConfigurationServiceClient.Instance.GetSetting("connectionStrings." + tenantId + ".artifacts");
            _checkpointSavedDataManager = new CheckpointSaveDataManager();
        }

        public string Tenant { get; private set; }
        public string EventStoreConnectionString { get; private set; }
        public string SourceOriginalConnectionString { get; private set; }
        public string SourceArtifactsConnectionString { get; private set; }
        public string DestinationOriginalConnectionString { get; private set; }
        public string DestinationArtifactsConnectionString { get; private set; }

        CheckpointSaveDataManager _checkpointSavedDataManager;

        public Int64 GetLastSyncedCheckpoint()
        {
            return _checkpointSavedDataManager.LoadTenantCheckpoint(Tenant);
        }

        public void SaveLastSyncedCheckpoint(Int64 checkpoint)
        {
            _checkpointSavedDataManager.SaveCheckpoint(Tenant, checkpoint);
        }



        internal void SetDestination(string destinationConnectionOriginal, string destinationConnectionArtifacts)
        {
            DestinationOriginalConnectionString = destinationConnectionOriginal;
            DestinationArtifactsConnectionString = destinationConnectionArtifacts;

            //validate mongo url, connection string must be different, destination should be in a different instance of mongo.
            var oriSourceMongoUrl = new MongoUrl(SourceOriginalConnectionString);
            var oriDestMongoUrl = new MongoUrl(DestinationOriginalConnectionString);
            if (oriSourceMongoUrl.Server.Host == oriDestMongoUrl.Server.Host &&
                oriSourceMongoUrl.Server.Port == oriDestMongoUrl.Server.Port)
            {
                Console.WriteLine("Destination database for Original blob is the same instance of Source db, this configuration is not supported");
                throw new ApplicationException("Destination database for Original blob is the same instance of Source db, this configuration is not supported");
            }

            var artSourceMongoUrl = new MongoUrl(SourceArtifactsConnectionString);
            var artDestMongoUrl = new MongoUrl(DestinationArtifactsConnectionString);
            if (artSourceMongoUrl.Server.Host == artDestMongoUrl.Server.Host &&
                artSourceMongoUrl.Server.Port == artDestMongoUrl.Server.Port)
            {
                Console.WriteLine("Destination database for Artifacts blob is the same instance of Source db, this configuration is not supported");
                throw new ApplicationException("Destination database for Artifacts blob is the same instance of Source db, this configuration is not supported");
            }
        }
    }



    static class FullArtifactSyncJob
    {



        internal static void StartSync()
        {
            //grab all tenants from configuration manager.
            Console.WriteLine("Reading data from configuration manager");

            var tenants = ConfigurationServiceClient.Instance.GetStructuredSetting("tenants");
            List<ArtifactSyncJobConfig> configs = new List<ArtifactSyncJobConfig>();
            foreach (string tenantId in tenants) // conversion from dynamic array
            {
                Console.WriteLine("Looking for configuration for tenant {0}", tenantId);

                var artifactSyncJobConfig = new ArtifactSyncJobConfig(tenantId);

                var tenantOriginalConnectionStringSettingName = tenantId + "-dest-ori";
                ConnectionStringSettings destinationConnectionOriginal = ConfigurationManager.ConnectionStrings[tenantOriginalConnectionStringSettingName];
                var tenantArtifactsConnectionStringSettingName = tenantId + "-dest-art";
                ConnectionStringSettings destinationConnectionArtifacts = ConfigurationManager.ConnectionStrings[tenantArtifactsConnectionStringSettingName];

                if (destinationConnectionOriginal == null)
                {
                    Console.WriteLine("Destination connection for original [{1}], tenant {0} not specified, tenant skipped", tenantId, tenantOriginalConnectionStringSettingName);
                    continue;
                }
                if (destinationConnectionArtifacts == null)
                {
                    Console.WriteLine("Destination connection for artifacts [{1}], tenant {0} not specified, tenant skipped", tenantId, tenantArtifactsConnectionStringSettingName);
                    continue;
                }
                Console.WriteLine("Configuration for tenant {0} valid!", tenantId);
                Console.WriteLine("Destination db for tenant/Original {0}: {1}", tenantId, destinationConnectionOriginal.ConnectionString);
                Console.WriteLine("Destination db for tenant/Artifacts {0}: {1}", tenantId, destinationConnectionArtifacts.ConnectionString);
                Console.WriteLine("TENANT {0} sync started from checkpoint token {1}", tenantId, artifactSyncJobConfig.GetLastSyncedCheckpoint());

                Console.Write("Do you want start syncronization for tenant {0} (s/n)?", tenantId);
                Char answer;
                do
                {
                    answer = Console.ReadKey().KeyChar;
                } while (answer != 's' && answer != 'n');
                if (answer == 's')
                {
                    artifactSyncJobConfig.SetDestination(destinationConnectionOriginal.ConnectionString, destinationConnectionArtifacts.ConnectionString);
                    configs.Add(artifactSyncJobConfig);
                }
            }
            Console.WriteLine("Press a key to start sync");
            Console.ReadLine();
            List<Task> tasks = new List<Task>();
            foreach (var config in configs)
            {
                var task = Task.Factory.StartNew(() => StartSyncingConfig(config));
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());

        }

        private static void StartSyncingConfig(ArtifactSyncJobConfig config)
        {
            var sourceMongoUrl = new MongoUrl(config.EventStoreConnectionString);
            var sourceDatabase = new MongoClient(sourceMongoUrl).GetDatabase(sourceMongoUrl.DatabaseName);

            var rmStream = sourceDatabase.GetCollection<BsonDocument>("Commits");
            Int64 checkpoint = config.GetLastSyncedCheckpoint();

            var syncUnit = new SyncUnit(new SyncUnitConfigurator());
            while (true)
            {

                var files = rmStream
                    .Find(Builders<BsonDocument>.Filter.Gte("_id", checkpoint))
                    .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
                    .ToEnumerable();
                foreach (var file in files)
                {
                    BsonArray events = file["Events"].AsBsonArray;
                    foreach (var evt in events)
                    {
                        var body = evt["Payload"]["Body"].AsBsonDocument;
                        if (body.Names.Contains("BlobId"))
                        {
                            string fileId = body["BlobId"].AsString;
                            syncUnit.Sync(fileId, config);
                        }
                    }

                    var checkpointToken = file["_id"].AsInt64;
                    checkpoint = checkpointToken + 1;
                    config.SaveLastSyncedCheckpoint(checkpoint);
                }
                Console.WriteLine("No more commit to sync!!");
                Thread.Sleep(2000);
            }
        }

        /// <summary>
        /// utility classes to read the rmStream collection with configuring the whole world
        /// </summary>
        [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
        internal class StreamReadModelDto
        {
            public long Id { get; internal set; }

            public HandleStreamEventTypes EventType { get; internal set; }

            public FormatInfo FormatInfo { get; internal set; }

            public FileNameWithExtension Filename { get; set; }

            internal class FileNameWithExtension
            {
                public string name { get; set; }

                public string ext { get; set; }
            }
        }

    }
    class CheckpointSaveDataManager
    {
        private string FilePath =
            Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "RmStreamArtifactSyncJob.rm-checkpoint.txt");


        public long LoadTenantCheckpoint(String tenantId)
        {
            try
            {
                var str = File.ReadAllText(GetTenantFileName(tenantId));
                return long.Parse(str);
            }
            catch
            {
                return 1;
            }
        }

        public void SaveCheckpoint(String tenantId, long checkpoint)
        {
            try
            {
                File.WriteAllText(GetTenantFileName(tenantId), checkpoint.ToString());
            }
            catch { }
        }

        private string GetTenantFileName(string tenantId)
        {
            return FilePath + "." + tenantId;
        }
    }


    internal class SyncUnitConfiguration
    {
        public string FileId { get; set; }
        public string Bucket { get; set; }
        public string SourceConnectionString { get; set; }
        public string DestConnectionString { get; set; }
    }

    internal class SyncUnitConfigurator
    {
        public SyncUnitConfiguration GetConfiguration(string fileId, ArtifactSyncJobConfig config)
        {
            string bucket = Path.GetFileNameWithoutExtension(fileId);
            if (bucket == "original")
            {
                return new SyncUnitConfiguration
                {
                    FileId = fileId,
                    Bucket = bucket,
                    SourceConnectionString = config.SourceOriginalConnectionString,
                    DestConnectionString = config.DestinationOriginalConnectionString
                };
            }
            return new SyncUnitConfiguration
            {
                FileId = fileId,
                Bucket = bucket,
                SourceConnectionString = config.SourceArtifactsConnectionString,
                DestConnectionString = config.DestinationArtifactsConnectionString
            };
        }
    }

    /// <summary>
    /// Given a single fileId will decide how to handle the copy
    /// </summary>
    internal class SyncUnit
    {
        private SyncUnitConfigurator _configurator;

        public SyncUnit(SyncUnitConfigurator configurator)
        {
            _configurator = configurator;
        }

        /// <summary>
        /// these information come from: rm.Stream
        /// - FormatInfo.BlobId
        /// - Filename.name
        /// - Filename.ext
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="dateTimeUploadFilter">I do not want to sync blob that are more recent than this 
        /// value, to avoid syncing blob while they are handled by DS</param>
        public Boolean Sync(
            string fileId,
            ArtifactSyncJobConfig config,
            DateTime? dateTimeUploadFilter = null)
        {
            var cfg = _configurator.GetConfiguration(fileId, config);

            // initialize Mongo Databases
            IGridFSBucket<string> sourceBucket;
            MongoUrl sourceMongoUrl;

            IGridFSBucket<string> destinationBucket;
            MongoUrl destinationMongoUrl;

            IMongoDatabase sourceDatabase;
            sourceMongoUrl = new MongoUrl(cfg.SourceConnectionString);
            sourceDatabase = new MongoClient(sourceMongoUrl).GetDatabase(sourceMongoUrl.DatabaseName);

            sourceBucket = new GridFSBucket<string>(sourceDatabase, new GridFSBucketOptions
            {
                BucketName = cfg.Bucket,
                ChunkSizeBytes = 1048576, // 1MB
            });

            IMongoDatabase destinationDatabase;
            destinationMongoUrl = new MongoUrl(cfg.DestConnectionString);
            destinationDatabase = new MongoClient(destinationMongoUrl).GetDatabase(destinationMongoUrl.DatabaseName);
            IMongoCollection<BsonDocument> destinationCollection = destinationDatabase.GetCollection<BsonDocument>(cfg.Bucket + ".files");

            destinationBucket = new GridFSBucket<string>(destinationDatabase, new GridFSBucketOptions
            {
                BucketName = cfg.Bucket,
                ChunkSizeBytes = 1048576, // 1MB
            });

            // before uploading the new element check if it's already in the destination database (maybe it's an alias)
            var findIdFilter = Builders<GridFSFileInfo<string>>.Filter.Eq(x => x.Id, fileId);
            using (var cursor = destinationBucket.Find(findIdFilter))
            {
                var exists = cursor.FirstOrDefault();
                if (exists != null)
                    return true; //Already synced, true 
            }

            var source = sourceBucket.Find(findIdFilter).FirstOrDefault();
            if (source == null)
            {
                return false; //source stream does not exists
            }

            if (dateTimeUploadFilter.HasValue && source.UploadDateTime > dateTimeUploadFilter)
                return false; //Consider this as not existing.

            var sw = Stopwatch.StartNew();
            Console.WriteLine("Sync needed Tenant {0}/{1}: ", config.Tenant, fileId);
            GridFSUploadOptions options = new GridFSUploadOptions();
            options.ChunkSizeBytes = source.ChunkSizeBytes;
            options.ContentType = source.ContentType;

            using (var destinationStream = destinationBucket.OpenUploadStream(fileId, source.Filename, options))
            {
                sourceBucket.DownloadToStream(fileId, destinationStream);
            }
            destinationCollection.UpdateOne(
                Builders<BsonDocument>.Filter.Eq("_id", fileId),
                Builders<BsonDocument>.Update.Set("uploadDate", source.UploadDateTime)
                );
            Console.WriteLine("DONE {0}/{1} ({2} ms)", config.Tenant, fileId, sw.ElapsedMilliseconds);
            sw.Stop();
            return true;
        }

    }

    ///// <summary>
    ///// This job keeps everything syncronyzed using the rm.stream as
    ///// source, this will prevent syncing duplicated artifacts.
    ///// </summary>
    //static class RmStreamArtifactSyncJob
    //{

    //    internal static void StartSync()
    //    {
    //        var checkPointManager = new Checkpoint();
    //        var checkpoint = checkPointManager.LoadRmCheckpoint();

    //        // start reading the rmStream collection (maybe in chuncks)
    //        var sourceMongoUrl = new MongoUrl(ArtifactSyncJobConfig.dsDocs_ConnectionString);
    //        var sourceDatabase = new MongoClient(sourceMongoUrl).GetDatabase(sourceMongoUrl.DatabaseName);
    //        var rmStream = sourceDatabase.GetCollection<StreamReadModelDto>("rm.Stream");
    //        var files = rmStream.AsQueryable()
    //            .Where(f => f.Id > checkpoint &&
    //                f.EventType == HandleStreamEventTypes.DocumentHasNewFormat)
    //            .OrderBy(f => f.Id);

    //        var syncUnit = new SyncUnit(new SyncUnitConfigurator());

    //        foreach (var file in files)
    //        {
    //            syncUnit.Sync(file.FormatInfo.BlobId);
    //            // very bad design, should work on a separate thread and persist this only if we stop
    //            // the job
    //            checkPointManager.SaveRmCheckpoint(file.Id);
    //        }
    //    }

    //    /// <summary>
    //    /// utility classes to read the rmStream collection with configuring the whole world
    //    /// </summary>
    //    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    //    internal class StreamReadModelDto
    //    {
    //        public long Id { get; internal set; }

    //        public HandleStreamEventTypes EventType { get; internal set; }

    //        public FormatInfo FormatInfo { get; internal set; }

    //        public FileNameWithExtension Filename { get; set; }

    //        internal class FileNameWithExtension
    //        {
    //            public string name { get; set; }

    //            public string ext { get; set; }
    //        }
    //    }
    //}
}
