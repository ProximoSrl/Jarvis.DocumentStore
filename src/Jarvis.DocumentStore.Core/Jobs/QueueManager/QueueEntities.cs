using CQRS.Shared.MultitenantSupport;
using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.ReadModel;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Jobs.QueueManager
{
    public class StreamCheckpoint
    {
        [BsonId]
        public TenantId TenantId { get; set; }

        public Int64 Checkpoint { get; set; }
    }

    public class QueueTenantInfo 
    {
        public TenantId TenantId { get; set; }

        public Int64 Checkpoint { get; set; }

        public IReader<StreamReadModel, Int64> StreamReader { get; set; }
    }
}
