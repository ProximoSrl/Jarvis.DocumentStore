using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.ProcessingPipeline
{
    public interface IPipelineManager
    {
        void FormatAvailable(PipelineId pipelineId, DocumentId documentId, DocumentFormat format, FileId fileId);
        void Start(DocumentId documentId, FileId fileId);
    }
}