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
        public ILogger Logger { get; set; }
        readonly IFileStore _fileStore;

        readonly IDictionary<PipelineId, IPipeline> _pipelines;
        readonly IJobHelper _jobHelper;

        public PipelineManager(IJobHelper jobHelper, IFileStore fileStore, IPipeline[] pipelines)
        {
            _jobHelper = jobHelper;
            _fileStore = fileStore;
            _pipelines = pipelines.ToDictionary(x=>x.Id, x=>x);
        }

        public void FormatAvailable(PipelineId pipelineId, DocumentId documentId, DocumentFormat format, FileId fileId)
        {
            var pipeline = _pipelines[pipelineId];
            pipeline.FormatAvailable(documentId, format, fileId);

            //switch (format)
            //{
            //    case DocumentFormats.Pdf:
            //        _jobHelper.QueueThumbnail(documentId, fileId);
            //        break;

            //    case DocumentFormats.RasterImage:
            //        _jobHelper.QueueResize(documentId,fileId);
            //        break;

            //    default:
            //        break;
            //}
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
