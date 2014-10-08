using System;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Quartz;

namespace Jarvis.DocumentStore.Core.ProcessingPipeline
{
    public interface IPipeline
    {
        PipelineId Id { get; }
        bool ShouldHandleFile(DocumentId documentId, IFileDescriptor filename);
        void Start(DocumentId documentId, IFileDescriptor descriptor);
        void FormatAvailable(DocumentId documentId, DocumentFormat format, FileId fileId);
    }

    public abstract class AbstractPipeline : IPipeline
    {
        public ILogger Logger { get; set; }
        public IJobHelper JobHelper { get; set; }
        public IPipelineManager PipelineManager { get; set; }
        
        protected AbstractPipeline(string id)
        {
            this.Id = new PipelineId(id);
        }

        public PipelineId Id { get; private set; }
        public abstract bool ShouldHandleFile(DocumentId documentId, IFileDescriptor filename);
        public abstract void Start(DocumentId documentId, IFileDescriptor descriptor);
        public abstract void FormatAvailable(DocumentId documentId, DocumentFormat format, FileId fileId);
    }
}