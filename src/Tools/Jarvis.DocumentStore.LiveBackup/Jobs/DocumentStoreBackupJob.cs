using Castle.Core;
using Castle.Core.Logging;
using Jarvis.DocumentStore.LiveBackup.BlobBackup;
using Jarvis.Framework.CommitBackup.Core;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.LiveBackup.Jobs
{
    /// <summary>
    /// Document store has the concept of tenant, so it is really simpler
    /// to use a job that scan all tenants and create backup for each tenant.
    /// </summary>
    public class DocumentStoreBackupJob : IStartable
    {
        private Timer _evenstoreTimer;

#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
#pragma warning disable IDE0052 // Remove unread private members
        private Timer _blobTimer;
#pragma warning restore IDE0052 // Remove unread private members
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables

        private readonly Support.Configuration _configuration;

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

        private readonly List<BackupData> _backupList;

        private readonly List<ArtifactSyncronizer> _artifactSyncronizerList;

        public DocumentStoreBackupJob(
            Support.Configuration configuration)
        {
            _configuration = configuration;
            _backupList = new List<BackupData>();
            _artifactSyncronizerList = new List<ArtifactSyncronizer>();
            Logger = NullLogger.Instance;
        }

        public void Start()
        {
            var readerFactory = _configuration.GetReaderFactory();
            var writerFactory = _configuration.GetWriterFactory();
            foreach (var tenantSetting in _configuration.TenantSettingList)
            {
                try
                {
                    Logger.InfoFormat("Started backup for tenant {0} db {1}", tenantSetting.TenantId, tenantSetting.EventStoreConnectionString);
                    MongoUrl url = new MongoUrl(tenantSetting.EventStoreConnectionString);
                    MongoClient client = new MongoClient(url);
                    var db = client.GetDatabase(url.DatabaseName);

                    var reader = readerFactory.Create(db);
                    var fileName = tenantSetting.GetEventsDumpFileName(_configuration.EventStoreBackupDirectory, url.DatabaseName);
                    var writer = writerFactory.Create(fileName);

                    _backupList.Add(new BackupData(reader, writer, tenantSetting.EventStoreConnectionString, tenantSetting.TenantId));

                    ArtifactSyncJobConfig artifactSyncConfig = new ArtifactSyncJobConfig(_configuration.BlobsBackupDirectory, tenantSetting, Logger);
                    _artifactSyncronizerList.Add(new ArtifactSyncronizer(artifactSyncConfig, Logger));
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
                        foreach (var commitInfo in b.Reader.GetCommits(last))
                        {
                            if (jobStopping) return;
                            if (Logger.IsDebugEnabled)
                                Logger.DebugFormat("Backup commit id {0} for {1}", commitInfo.Commit["_id"], b.DatabaseInfo);
                            b.Writer.Append(commitInfo.Commit["_id"].AsInt64, commitInfo);
                        }
                        b.Writer.Close();
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error during backup polling: {ex.Message}.", ex);
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
                    Parallel.ForEach(_artifactSyncronizerList, b => b.PerformSync());
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
