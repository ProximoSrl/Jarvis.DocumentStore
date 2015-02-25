using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Shared.IdentitySupport.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor
{
    [BsonSerializer(typeof(EventStoreIdentityBsonSerializer))]
    public class DocumentDescriptorId : EventStoreIdentity
    {
        [JsonConstructor]
        public DocumentDescriptorId(string id)
            : base(id)
        {
        }

        public DocumentDescriptorId(long id)
            : base(id)
        {
        }
    }
}