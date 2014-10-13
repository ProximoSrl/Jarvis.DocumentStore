using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Services
{
    public interface IFileHandleMapper
    {
        void Associate(FileHandle handle, DocumentId documentId);
    }
}