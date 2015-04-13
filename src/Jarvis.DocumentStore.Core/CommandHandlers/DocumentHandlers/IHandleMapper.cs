using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public interface IHandleMapper
    {
        DocumentId Map(DocumentHandle handle);
    }
}