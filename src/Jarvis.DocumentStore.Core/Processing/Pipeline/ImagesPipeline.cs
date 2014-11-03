using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.Processing.Pipeline
{
    public class ImagesPipeline : AbstractPipeline
    {
        private readonly string[] _formats;
        public ImagesPipeline() : base("img")
        {
            _formats = "png|jpg|jpeg".Split('|');
        }

        public override bool ShouldHandleFile(DocumentId documentId, IBlobDescriptor storeDescriptor)
        {
            return _formats.Contains(storeDescriptor.FileNameWithExtension.Extension);
        }

        protected override void OnStart(DocumentId documentId, IBlobDescriptor storeDescriptor)
        {
            JobHelper.QueueResize(
                Id, 
                documentId, 
                storeDescriptor.BlobId,
                storeDescriptor.FileNameWithExtension.Extension
            );
        }

        protected override void OnFormatAvailable(DocumentId documentId, DocumentFormat format, IBlobDescriptor descriptor)
        {
            Logger.DebugFormat("{0}: new format available {1}", documentId, format);
        }
    }
}
