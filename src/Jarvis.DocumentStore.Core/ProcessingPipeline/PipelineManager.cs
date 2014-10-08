using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Quartz;

namespace Jarvis.DocumentStore.Core.ProcessingPipeline
{
    public class PipelineManager : IPipelineManager
    {
        ILogger _logger;
        readonly IFileStore _fileStore;

        readonly IDictionary<PipelineId, IPipeline> _pipelines;

        public PipelineManager(IFileStore fileStore, IPipeline[] pipelines, ILogger logger)
        {
            _fileStore = fileStore;
            _logger = logger;
            _pipelines = pipelines.ToDictionary(x=>x.Id, x=>x);

            _logger.Debug("Configuring pipelines");
            foreach (var pipeline in pipelines)
            {
                _logger.DebugFormat("...added {0}", pipeline.Id);
                pipeline.Attach(this);
            }
            _logger.Debug("Pipelines config done");
        }

        public void FormatAvailable(PipelineId pipelineId, DocumentId documentId, DocumentFormat format, FileId fileId)
        {
            var pipeline = _pipelines[pipelineId];
            pipeline.FormatAvailable(documentId, format, fileId);
        }

        public void Start(DocumentId documentId, FileId fileId)
        {
            var descriptor = _fileStore.GetDescriptor(fileId);

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
