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

        public override bool ShouldHandleFile(DocumentId documentId, IFileStoreDescriptor storeDescriptor)
        {
            return _formats.Contains(storeDescriptor.FileNameWithExtension.Extension);
        }

        protected override void OnStart(DocumentId documentId, IFileStoreDescriptor storeDescriptor)
        {
            JobHelper.QueueResize(
                Id, 
                documentId, 
                storeDescriptor.FileId,
                storeDescriptor.FileNameWithExtension.Extension
            );
        }

        protected override void OnFormatAvailable(DocumentId documentId, DocumentFormat format, IFileStoreDescriptor descriptor)
        {
            Logger.DebugFormat("{0}: new format available {1}", documentId, format);
        }
    }
}
