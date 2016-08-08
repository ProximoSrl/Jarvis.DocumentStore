using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Shared.IdentitySupport.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    [BsonSerializer(typeof(TypedEventStoreIdentityBsonSerializer<DocumentId>))]
    public class DocumentId : EventStoreIdentity
    {
        [JsonConstructor]
        public DocumentId(string id)
            : base(id)
        {
        }

        public DocumentId(long id)
            : base(id)
        {
        }
    }
}
