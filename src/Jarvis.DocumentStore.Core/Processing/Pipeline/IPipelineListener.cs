using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.Processing.Pipeline
{
    public interface IPipelineListener
    {
        void OnStart(IPipeline pipeline, DocumentId documentId, IFileStoreDescriptor storeDescriptor);
        void OnFormatAvailable(IPipeline pipeline, DocumentId documentId, DocumentFormat format, IFileStoreDescriptor storeDescriptor);
    }
}