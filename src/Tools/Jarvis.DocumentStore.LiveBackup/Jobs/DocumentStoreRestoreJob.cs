using Castle.Core.Logging;

namespace Jarvis.DocumentStore.LiveBackup.Jobs
{
    /// <summary>
    /// TODO: NEEd to refactor this class to use a different way to restore 
    /// database.
    /// Probably we do not need a restore job, but a shell command that is
    /// capable of restoring the job.
    /// </summary>
    public class DocumentStoreRestoreJob
    {
        private readonly Support.Configuration _configuration;

        public ILogger Logger { get; set; }

        public DocumentStoreRestoreJob(
            Support.Configuration configuration)
        {
            _configuration = configuration;
            Logger = NullLogger.Instance;
        }

        public void Start()
        {
            //foreach (var tenantSetting in _configuration.TenantSettingList)
            //{
            //    Logger.InfoFormat("Started restore for database {0}", tenantSetting.EventStoreConnectionString);
            //    MongoUrl url = new MongoUrl(tenantSetting.EventStoreConnectionString);
            //    MongoClient client = new MongoClient(url);
            //    var db = client.GetDatabase(url.DatabaseName);

            //    IMongoCollection<BsonDocument> restoredCollection = db.GetCollection<BsonDocument>("Commits_Restored");
            //    var FileName = Path.Combine(_configuration.EventStoreBackupDirectory, url.DatabaseName + ".dump");
            //    var writer = _fileWriterFactory(FileName, _configuration.MaxFileSize);
            //    db.DropCollection(restoredCollection.CollectionNamespace.CollectionName);

            //    Logger.InfoFormat("Restore all commits with Mongodb Batch insertion");
            //    restoredCollection.InsertMany(writer.GetCommits(0L).Select(cinfo => cinfo.Commit));
            //}
        }

        public void Stop()
        {
            // Method intentionally left empty.
        }
    }
}
