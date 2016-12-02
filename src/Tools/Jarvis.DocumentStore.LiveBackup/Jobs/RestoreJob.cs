using Castle.Core.Logging;
using Jarvis.Framework.CommitBackup.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.LiveBackup.Jobs
{
    public class RestoreJob
    {
        private Func<IMongoDatabase, ICommitReader> _readerFactory;

        private Func<String, Int64, ICommitWriter> _fileWriterFactory;

        private Support.Configuration _configuration;

        public ILogger Logger { get; set; }


        public RestoreJob(
            Func<IMongoDatabase, ICommitReader> readerFactory,
            Func<String, Int64, ICommitWriter> fileWriterFactory,
            Support.Configuration configuration)
        {
            _readerFactory = readerFactory;
            _fileWriterFactory = fileWriterFactory;
            _configuration = configuration;
            Logger = NullLogger.Instance;
        }

        public void Start()
        {
            foreach (var tenantSetting in _configuration.TenantSettingList)
            {
                Logger.InfoFormat("Started restore for database {0}", tenantSetting.EventStoreConnectionString);
                MongoUrl url = new MongoUrl(tenantSetting.EventStoreConnectionString);
                MongoClient client = new MongoClient(url);
                var db = client.GetDatabase(url.DatabaseName);

                IMongoCollection<BsonDocument> restoredCollection = db.GetCollection<BsonDocument>("Commits_Restored");
                var FileName = Path.Combine(_configuration.EventStoreBackupDirectory, url.DatabaseName + ".dump");
                var writer = _fileWriterFactory(FileName, _configuration.MaxFileSize);
                db.DropCollection(restoredCollection.CollectionNamespace.CollectionName);

                Logger.InfoFormat("Restore all commits with Mongodb Batch insertion");
                restoredCollection.InsertMany(writer.GetCommits(0L));
            }
        }

        public void Stop()
        {

        }
    }
}
