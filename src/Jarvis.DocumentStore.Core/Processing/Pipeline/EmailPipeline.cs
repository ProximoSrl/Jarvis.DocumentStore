using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.Processing.Pipeline
{
    public class EmailPipeline : AbstractPipeline
    {
        private readonly string[] _formats;
        public EmailPipeline() : base("email")
        {
            _formats = "eml|msg".Split('|');
        }

        public override bool ShouldHandleFile(DocumentId documentId, IBlobDescriptor storeDescriptor, IPipeline fromPipeline)
        {
            return _formats.Contains(storeDescriptor.FileNameWithExtension.Extension);
        }

        protected override void OnStart(DocumentId documentId, IBlobDescriptor storeDescriptor)
        {
            Logger.DebugFormat("Processing email {0}", storeDescriptor.FileNameWithExtension);
            JobHelper.QueueEmailToHtml(Id, documentId, storeDescriptor.BlobId);
        }

        protected override void OnFormatAvailable(DocumentId documentId, DocumentFormat format, IBlobDescriptor descriptor)
        {
            Logger.DebugFormat("Email {0} has been converted to format {1}: {2}", documentId, format, descriptor.BlobId);
            PipelineManager.Start(documentId, descriptor, this);
        }
    }
}
