using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using CQRS.Kernel.Events;
using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.ProcessingPipeline;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class PipelineHandler : AbstractProjection
        ,IEventHandler<DocumentCreated>
        ,IEventHandler<FormatAddedToDocument>
    {
        readonly IFileStore _fileStore;
        public ILogger Logger { get; set; }
        IConversionWorkflow _conversionWorkflow;

        public PipelineHandler(IFileStore fileStore, IConversionWorkflow conversionWorkflow)
        {
            _fileStore = fileStore;
            _conversionWorkflow = conversionWorkflow;
        }

        public override void Drop()
        {
        }
        public override void SetUp()
        {
        }

        public void On(DocumentCreated e)
        {
            var descriptor = _fileStore.GetDescriptor(e.FileId);
            Logger.DebugFormat("Starting conversion of document {0} {1}", e.FileId, descriptor.FileName);
            _conversionWorkflow.Start((DocumentId)e.AggregateId, e.FileId);
        }

        public void On(FormatAddedToDocument e)
        {
            var descriptor = _fileStore.GetDescriptor(e.FileId);
            Logger.DebugFormat("Next conversion step for document {0} {1}", e.FileId, descriptor.FileName);
            _conversionWorkflow.FormatAvailable((DocumentId)e.AggregateId, e.DocumentFormat, e.FileId);
        }
    }
}
