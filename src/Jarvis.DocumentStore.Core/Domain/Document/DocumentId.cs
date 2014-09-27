using CQRS.Shared.IdentitySupport;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    public class DocumentId : EventStoreIdentity
    {
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