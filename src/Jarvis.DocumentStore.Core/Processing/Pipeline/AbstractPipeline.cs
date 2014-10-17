using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.Processing.Pipeline
{
    public abstract class AbstractPipeline : IPipeline
    {
        public ILogger Logger { get; set; }
        public IJobHelper JobHelper { get; set; }
        protected IPipelineManager PipelineManager { get; private set; }

        public IPipelineListener[] Listeners { get; set; }

        protected AbstractPipeline(string id)
        {
            this.Id = new PipelineId(id);
        }

        public PipelineId Id { get; private set; }
        public abstract bool ShouldHandleFile(DocumentId documentId, IFileDescriptor descriptor);

        public void Start(DocumentId documentId, IFileDescriptor descriptor)
        {
            OnStart(documentId, descriptor);
            if (Listeners != null)
            {
                foreach (var pipelineListener in Listeners)
                {
                    pipelineListener.OnStart(this, documentId, descriptor);
                }
            }
        }

        protected abstract void OnStart(DocumentId documentId, IFileDescriptor descriptor);

        public void FormatAvailable(DocumentId documentId, DocumentFormat format, FileId fileId)
        {
            OnFormatAvailable(documentId, format, fileId);
            if (Listeners != null)
            {
                foreach (var pipelineListener in Listeners)
                {
                    pipelineListener.OnFormatAvailable(this, documentId, format, fileId);
                }
            }
        }

        protected abstract void OnFormatAvailable(DocumentId documentId, DocumentFormat format, FileId fileId);
        
        public void Attach(IPipelineManager manager)
        {
            this.PipelineManager = manager;
        }
    }
}