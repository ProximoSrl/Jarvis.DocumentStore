using Castle.Core;
using Castle.Core.Logging;
using Castle.Windsor.Installer;
using Jarvis.DocumentStore.LiveBackup.BlobBackup;
using Jarvis.Framework.CommitBackup.Core;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.LiveBackup.Jobs
{
    public class BackupJob : IStartable
    {
        private Timer _evenstoreTimer;

        private Timer _blobTimer;

        private Func<IMongoDatabase, ICommitReader> _readerFactory;

        private Func<String, Int64, ICommitWriter> _fileWriterFactory;

        private Support.Configuration _configuration;

        public ILogger Logger { get; set; }

        private struct BackupData
        {
            public BackupData(ICommitReader reader, ICommitWriter writer, string databaseInfo, string tenant) : this()
            {
                Reader = reader;
                Writer = writer;
                DatabaseInfo = databaseInfo;
                Tenant = tenant;
            }

            public ICommitReader Reader { get; private set; }
            public ICommitWriter Writer { get; private set; }
            public String DatabaseInfo { get; private set; }
            public String Tenant { get; private set; }
        }
        private List<BackupData> _backupList;

        private List<ArtifactSyncronizer> _artifactSyncronizerList;

        public BackupJob(
            Func<IMongoDatabase, ICommitReader> readerFactory,
            Func<String, Int64, ICommitWriter> fileWriterFactory,
            Support.Configuration configuration)
        {
            _readerFactory = readerFactory;
            _fileWriterFactory = fileWriterFactory;
            _configuration = configuration;
            _backupList = new List<BackupData>();
            _artifactSyncronizerList = new List<ArtifactSyncronizer>();
            Logger = NullLogger.Instance;
        }

        public void Start()
        {
            foreach (var tenantSetting in _configuration.TenantSettingList)
            {
                try
                {
                    Logger.InfoFormat("Started backup for tenant {0} db {1}", tenantSetting.TenantId, tenantSetting.EventStoreConnectionString);
                    MongoUrl url = new MongoUrl(tenantSetting.EventStoreConnectionString);
                    MongoClient client = new MongoClient(url);
                    var db = client.GetDatabase(url.DatabaseName);

                    var reader = _readerFactory(db);
                    var fileName = tenantSetting.GetEventsDumpFileName(_configuration.EventStoreBackupDirectory, url.DatabaseName);
                    var writer = _fileWriterFactory(fileName, _configuration.MaxFileSize);

                    _backupList.Add(new BackupData(reader, writer, tenantSetting.EventStoreConnectionString, tenantSetting.TenantId));

                    ArtifactSyncJobConfig artifactSyncConfig = new ArtifactSyncJobConfig(_configuration.BlobsBackupDirectory, tenantSetting);
                    _artifactSyncronizerList.Add(new ArtifactSyncronizer(artifactSyncConfig));
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat(ex, "Error during setup of live backup for tenant {0}", tenantSetting.TenantId);
                }
            }
            _evenstoreTimer = new Timer(DoPollForEventStoreContent, null, 0, 5000);
            _blobTimer = new Timer(DoPollForBlobContent, null, 0, 5000);
        }

        public void Stop()
        {
            _evenstoreTimer.Dispose();
            jobStopping = true;
            //wait for all job to close
            while (_isEventStoreTimerPolling > 0)
                Thread.Sleep(100);
            foreach (var item in _backupList)
            {
                item.Writer.Close();
            }
        }

        private Int32 _isEventStoreTimerPolling;
        private Int32 _isBlobTimerPolling;

        private Boolean jobStopping = false;

        private void DoPollForEventStoreContent(object state)
        {
            if (Interlocked.CompareExchange(ref _isEventStoreTimerPolling, 1, 0) == 0)
            {
                Logger.Debug("Polling!");
                try
                {
                    Parallel.ForEach(_backupList, b =>
                    {
                        var last = b.Writer.GetLastCommitAppended();
                        foreach (var commit in b.Reader.GetCommits(last))
                        {
                            if (jobStopping) return;
                            if (Logger.IsDebugEnabled)
                                Logger.DebugFormat("Backup commit id {0} for {1}", commit["_id"], b.DatabaseInfo);
                            b.Writer.Append(commit["_id"].AsInt64, commit);
                        }
                        b.Writer.Close();
                    });
                }
                finally
                {
                    Logger.Debug("Finished polling");
                    Interlocked.Exchange(ref _isEventStoreTimerPolling, 0);
                }
            }
        }

        private void DoPollForBlobContent(object state)
        {
            if (Interlocked.CompareExchange(ref _isBlobTimerPolling, 1, 0) == 0)
            {
                Logger.Debug("Polling!");
                try
                {
                    Parallel.ForEach(_artifactSyncronizerList, b =>
                    {
                        b.PerformSync();
                    });
                }
                finally
                {
                    Logger.Debug("Finished polling");
                    Interlocked.Exchange(ref _isBlobTimerPolling, 0);
                }
            }
        }
    }
}
