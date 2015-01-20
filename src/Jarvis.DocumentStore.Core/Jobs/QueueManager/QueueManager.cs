﻿using Castle.Core;
using Castle.Core.Logging;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.Support;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Jobs.QueueManager
{
    public class QueueManager : IStartable
    {
        private DocumentStoreConfiguration _configuration;
        private ITenantAccessor _tenantAccessor;
        private Thread _pollerThread;
        private BlockingCollection<CommandData> _commandList;
        private System.Timers.Timer pollerTimer;

        private MongoCollection _checkpointCollection;

        private ITenant[] tenants;

        public ILogger Logger { get; set; }

        public QueueManager(
            MongoDatabase mongoDatabase, 
            ITenantAccessor tenantAccessor,
            DocumentStoreConfiguration configuration)
        {
            _tenantAccessor = tenantAccessor;
            tenants = tenantAccessor.Tenants;
            _commandList = new BlockingCollection<CommandData>();
            _configuration = configuration;
            _checkpointCollection = mongoDatabase.GetCollection<StreamCheckpoint>("queue.checkpoints");
            Logger = NullLogger.Instance;
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
            
        }

        public void PollNow() 
        {
            _commandList.Add(CommandData.Poll());
        }

        private void TimerCallback(object sender, System.Timers.ElapsedEventArgs e)
        {
            _commandList.Add(CommandData.Poll());
        }

    }
}