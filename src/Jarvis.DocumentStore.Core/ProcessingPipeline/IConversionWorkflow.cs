using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.ProcessingPipeline
{
    public interface IConversionWorkflow
    {
        void Next(DocumentId documentId, FileId fileId, string nextJob);
        void Start(DocumentId documentId, FileId fileId);
    }
}