using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.Processing.Pipeline
{
    public interface IPipeline
    {
        PipelineId Id { get; }
        bool ShouldHandleFile(DocumentId documentId, IFileStoreDescriptor storeDescriptor);
        void Start(DocumentId documentId, IFileStoreDescriptor storeDescriptor);
        void FormatAvailable(DocumentId documentId, DocumentFormat format, IFileStoreDescriptor storeDescriptor);
        void Attach(IPipelineManager manager);
    }

    public interface IPipelineListener
    {
        void OnStart(IPipeline pipeline, DocumentId documentId, IFileStoreDescriptor storeDescriptor);
        void OnFormatAvailable(IPipeline pipeline, DocumentId documentId, DocumentFormat format, IFileStoreDescriptor storeDescriptor);
    }
}