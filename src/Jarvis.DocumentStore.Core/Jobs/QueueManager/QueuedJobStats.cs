using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Shared.Jobs;

namespace Jarvis.DocumentStore.Core.Jobs
{

    public class QueuedJobStatus
    {
        public sealed class QueuedJobStatInfo
        {
            public String Group { get; set; }

            public String Status { get; private set; }

            public long Count { get; private set; }

            public QueuedJobStatInfo(String queueName, QueuedJobExecutionStatus status, long count)
            {
                Group = queueName;
                Status = status.ToString();
                Count = count;
            }
        }

        readonly QueueHandler[] _queueHandlers;

        public ILogger Logger { get; set; }

        public QueuedJobStatus(
            QueueHandler[] queueHandlers)
        {
            _queueHandlers = queueHandlers;
            Logger = NullLogger.Instance;
        }

        public List<QueuedJobStatInfo> GetQueuesStatus()
        {
            var retValue = new List<QueuedJobStatInfo>();
            foreach (var qh in _queueHandlers)
            {
                var queueStats = qh.GetStatus();
                retValue.AddRange(queueStats.Select(s =>
                    new QueuedJobStatInfo(qh.Name, s.Status, s.Count)));
            }
            return retValue;
        }
    }
}
