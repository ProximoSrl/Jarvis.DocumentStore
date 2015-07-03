using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.CommandHandlers.DocumentHandlers
{
    public interface IHandleMapper
    {
        DocumentId Map(DocumentHandle handle);

        /// <summary>
        /// Remove an handle in the system.
        /// </summary>
        /// <param name="handle"></param>
        void DeleteHandle(DocumentHandle handle);

        DocumentId TryTranslate(string externalKey);
    }
}