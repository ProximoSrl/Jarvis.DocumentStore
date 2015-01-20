using CQRS.Shared.MultitenantSupport;
using MongoDB.Bson.Serialization.Attributes;
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
}
