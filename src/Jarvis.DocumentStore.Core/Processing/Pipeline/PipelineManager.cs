using System.Collections.Generic;
using System.Linq;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Core.Support;

namespace Jarvis.DocumentStore.Core.Processing.Pipeline
{
    public class PipelineManager : IPipelineManager
    {
        ILogger _logger;
        readonly IDictionary<PipelineId, IPipeline> _pipelines;
        readonly DocumentStoreConfiguration _config;

        public PipelineManager(IPipeline[] pipelines, ILogger logger, DocumentStoreConfiguration config)
        {
            _logger = logger;
            _pipelines = pipelines.ToDictionary(x=>x.Id, x=>x);
            _config = config;

            _logger.Debug("Configuring pipelines");
            foreach (var pipeline in pipelines)
            {
                _logger.DebugFormat("...adding {0}", pipeline.Id);
                pipeline.Attach(this);
            }
            _logger.Debug("Pipelines config done");
        }

        public void FormatAvailable(PipelineId pipelineId, DocumentId documentId, DocumentFormat format, IBlobDescriptor descriptor)
        {
            if (_config.JobMode != JobModes.Quartz) return;
            var pipeline = _pipelines[pipelineId];
            pipeline.FormatAvailable(documentId, format, descriptor);
        }

        public void Start(DocumentId documentId, IBlobDescriptor descriptor, IPipeline fromPipeline)
        {
            if (_config.JobMode != JobModes.Quartz) return;
            foreach (var pipeline in _pipelines.Values)
            {
                if (pipeline.ShouldHandleFile(documentId, descriptor, fromPipeline))
                {
                    pipeline.Start(documentId, descriptor);
                }
            }
        }
    }
}
