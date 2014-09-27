using CQRS.Shared.IdentitySupport;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    public class FormatId : LowercaseStringId
    {
        public FormatId(string id)
            : base(id)
        {
        }
    }
}