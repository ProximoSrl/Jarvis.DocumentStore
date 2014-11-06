using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.Processing.Pipeline
{
    public interface IPipeline
    {
        PipelineId Id { get; }
        bool ShouldHandleFile(DocumentId documentId, IBlobDescriptor storeDescriptor, IPipeline fromPipeline);
        void Start(DocumentId documentId, IBlobDescriptor storeDescriptor);
        void FormatAvailable(DocumentId documentId, DocumentFormat format, IBlobDescriptor storeDescriptor);
        void Attach(IPipelineManager manager);
    }
}