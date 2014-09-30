using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.ProcessingPipeline
{
    public interface IConversionWorkflow
    {
        void FormatAvailable(DocumentId documentId, DocumentFormat format, FileId fileId);
        void Start(DocumentId documentId, FileId fileId);
    }
}