using CQRS.Shared.Domain;
using CQRS.Shared.IdentitySupport;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    public class FormatValue : LowercaseStringValue
    {
        public FormatValue(string value)
            : base(value)
        {
        }
    }
}