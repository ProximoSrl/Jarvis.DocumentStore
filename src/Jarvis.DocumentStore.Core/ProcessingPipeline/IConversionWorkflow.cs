using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.ProcessingPipeline
{
    public interface IConversionWorkflow
    {
        void Next(FileId fileId, string nextJob);
        void Start(FileId fileId);
    }
}