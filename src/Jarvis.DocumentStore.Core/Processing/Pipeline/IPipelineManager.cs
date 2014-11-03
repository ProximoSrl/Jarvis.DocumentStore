using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.Processing.Pipeline
{
    public interface IPipelineManager
    {
        void FormatAvailable(
            PipelineId pipelineId, 
            DocumentId documentId, 
            DocumentFormat format,
            IFileStoreDescriptor descriptor
        );
        void Start(DocumentId documentId, IFileStoreDescriptor descriptor);
    }
}