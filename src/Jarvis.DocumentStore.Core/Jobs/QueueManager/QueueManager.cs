using Castle.Core;
using Castle.Core.Logging;
using CQRS.Shared.MultitenantSupport;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Support;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Jobs.QueueManager
{

    public class QueueManager 
    {
        private DocumentStoreConfiguration _configuration;
        private ITenantAccessor _tenantAccessor;
        private Thread _pollerThread;
        private BlockingCollection<CommandData> _commandList;
        private System.Timers.Timer pollerTimer;

        private MongoCollection<StreamCheckpoint> _checkpointCollection;

        private QueueTenantInfo[] _queueTenantInfos;

        private QueueHandler[] _queueHandlers;

        public ILogger Logger { get; set; }

        public QueueManager(
            MongoDatabase mongoDatabase,
            ITenantAccessor tenantAccessor,
            DocumentStoreConfiguration configuration)
        {
            _tenantAccessor = tenantAccessor;
            _configuration = configuration;
            _checkpointCollection = mongoDatabase.GetCollection<StreamCheckpoint>("queue.checkpoints");

            _queueTenantInfos = tenantAccessor.Tenants
                .Select(t => new QueueTenantInfo()
                {
                    TenantId = t.Id,
                    Checkpoint = FindLastCheckpointForTenant(t.Id),
                    StreamReader = t.Container.Resolve<IReader<StreamReadModel, Int64>>(),
                })
                .ToArray();
            _commandList = new BlockingCollection<CommandData>();
            _queueHandlers = _configuration.QueueInfoList
                .Select(qil => new QueueHandler(qil, mongoDatabase))
                .ToArray();
            Logger = NullLogger.Instance;
        }

        private long FindLastCheckpointForTenant(TenantId tenantId)
        {
            var dbCheckpoint = _checkpointCollection.Find(
                        Query<StreamCheckpoint>.EQ(t => t.TenantId, tenantId)
                   ).SingleOrDefault();
            return dbCheckpoint != null ? dbCheckpoint.Checkpoint : 0L;
        }

        private class CommandData
        {
            public QueueCommands Command { get; set; }

            internal static CommandData Poll()
            {
                return new CommandData() { Command = QueueCommands.Poll };
            }
        }

        private enum QueueCommands
        {
            Poll = 0
        }

        public void Start()
        {
            _pollerThread = new Thread(PollerFunc);
            _pollerThread.IsBackground = true;
            _pollerThread.Start();

            pollerTimer = new System.Timers.Timer(_configuration.QueueStreamPollTime);
            pollerTimer.Elapsed += TimerCallback;
            pollerTimer.Start();
        }

        public void Stop()
        {
            _commandList.CompleteAdding();
            _commandList.Dispose();
        }

        private void PollerFunc(object obj)
        {
            foreach (var command in _commandList.GetConsumingEnumerable())
            {
                try
                {
                    switch (command.Command)
                    {
                        case QueueCommands.Poll:
                            pollerTimer.Stop(); //no need to add more polling if we are already polling
                            Poll();
                            break;

                        default:
                            Logger.ErrorFormat("Unknown command {0}", command.Command);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat(ex, "Error in executing command: {0}", command.Command);
                    throw;
                }
                finally
                {
                    if (!pollerTimer.Enabled) pollerTimer.Start();
                }
            }
        }

        private void Poll()
        {
            //do a poll for every tenant.
            Boolean hasNewData = false;
            do
            {
                foreach (var info in _queueTenantInfos)
                {
                    var blockOfStreamData = info.StreamReader.AllSortedById
                        .Where(s => s.Id > info.Checkpoint)
                        .Take(50)
                        .ToList();
                    if (blockOfStreamData.Count > 0) 
                    {
                        hasNewData = true;
                        foreach (var streamData in blockOfStreamData)
                        {
                            foreach (var qh in _queueHandlers)
                            {
                                //In this version we are interested only in event for new formats
                                if (streamData.EventType != HandleStreamEventTypes.HandleHasNewFormat) continue;
                                qh.Handle(streamData, info.TenantId);
                            }
                        }
                        info.Checkpoint = blockOfStreamData[blockOfStreamData.Count -1].Id;
                        _checkpointCollection.Save(new StreamCheckpoint() {Checkpoint = info.Checkpoint, TenantId = info.TenantId});
                    }
                }
            } while (hasNewData);
        }

        public void PollNow()
        {
            if (!_commandList.Any(c => c.Command == QueueCommands.Poll))
                _commandList.Add(CommandData.Poll());
        }

        private void TimerCallback(object sender, System.Timers.ElapsedEventArgs e)
        {
            PollNow();
        }

    }
}
