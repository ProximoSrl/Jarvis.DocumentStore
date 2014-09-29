using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Services
{
    public interface IDocumentMapper
    {
        DocumentId Map(FileId fileId);
    }
}