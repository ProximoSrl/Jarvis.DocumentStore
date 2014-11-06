using Jarvis.DocumentStore.Core.Domain.Handle;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.CommandHandlers.HandleHandlers
{
    public interface IHandleMapper
    {
        HandleId Map(DocumentHandle handle);
    }
}