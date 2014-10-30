using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.Processing.Pipeline.Listeners
{
    public class LoggingPipelineListener : IPipelineListener
    {
        public ILogger Logger { get; set; }
        public void OnStart(IPipeline pipeline, DocumentId documentId, IFileStoreDescriptor storeDescriptor)
        {
            Logger.DebugFormat(
                "OnStart pipeline {0} with {1} (File {2})",
                pipeline.Id,
                documentId,
                storeDescriptor.FileId
            );
        }

        public void OnFormatAvailable(IPipeline pipeline, DocumentId documentId, DocumentFormat format, FileId fileId)
        {
            Logger.DebugFormat(
                "OnFormatAvailable pipeline {0}. {1} format {2} (File {3})",
                pipeline.Id,
                documentId,
                format,
                fileId
            );
        }
    }
}
