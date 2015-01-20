using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.ReadModel;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Jobs.QueueManager
{
    public interface IQueueHandler 
    {
        void Handle(StreamReadModel streamElement);
    }

    public class QueueInfo 
    {
        /// <summary>
        /// name of the queue
        /// </summary>
        public String Name { get; set; }

        /// <summary>
        /// It is a .NET regex
        /// If different from null it contains a filter that permits to queue jobs
        /// only if the <see cref="StreamReadModel" /> is generated from a specific
        /// pipeline.
        /// </summary>
        public String Pipeline { get; set; }

        /// <summary>
        /// It is a pipe separated list of desired extension.
        /// </summary>
        public String Extension { get; set; }
    }

    public class QueuedJob 
    {
        public Int64 Id { get; set; }

        public TenantId TenantId { get; set; }
    }

    /// <summary>
    /// This is a simple handler for queue.
    /// </summary>
    public class QueueHandler : IQueueHandler
    {
        MongoCollection<QueuedJob> _collection;
        QueueInfo _info;

        public QueueHandler(QueueInfo info, MongoDatabase database)
        {
            _collection = database.GetCollection<QueuedJob>("queue-" + info.Name);
            _info = info;
        }

        public void Handle(StreamReadModel streamElement)
        {
            
        }
    }
}
