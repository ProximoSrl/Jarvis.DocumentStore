using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Shared.IdentitySupport.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Jarvis.DocumentStore.Core.Domain.Handle
{
    [BsonSerializer(typeof(EventStoreIdentityBsonSerializer))]
    public class HandleId : EventStoreIdentity
    {
        [JsonConstructor]
        public HandleId(string id)
            : base(id)
        {
        }

        public HandleId(long id)
            : base(id)
        {
        }
    }
}
