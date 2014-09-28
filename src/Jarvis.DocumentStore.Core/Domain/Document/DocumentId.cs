using CQRS.Shared.IdentitySupport;
using Newtonsoft.Json;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
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