using System.Collections.Generic;
using System.Linq;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.Processing.Pipeline
{
    public class PipelineManager : IPipelineManager
    {
        ILogger _logger;
        readonly IDictionary<PipelineId, IPipeline> _pipelines;

        public PipelineManager(IPipeline[] pipelines, ILogger logger)
        {
            _logger = logger;
            _pipelines = pipelines.ToDictionary(x=>x.Id, x=>x);

            _logger.Debug("Configuring pipelines");
            foreach (var pipeline in pipelines)
            {
                _logger.DebugFormat("...adding {0}", pipeline.Id);
                pipeline.Attach(this);
            }
            _logger.Debug("Pipelines config done");
        }

        public void FormatAvailable(PipelineId pipelineId, DocumentId documentId, DocumentFormat format, IFileStoreDescriptor descriptor)
        {
            var pipeline = _pipelines[pipelineId];
            pipeline.FormatAvailable(documentId, format, descriptor);
        }

        public void Start(DocumentId documentId, IFileStoreDescriptor descriptor)
        {
            foreach (var pipeline in _pipelines.Values)
            {
                if (pipeline.ShouldHandleFile(documentId, descriptor))
                {
                    pipeline.Start(documentId, descriptor);
                }
            }
        }
    }
}
