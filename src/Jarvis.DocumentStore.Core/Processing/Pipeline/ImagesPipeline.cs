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

        public override bool ShouldHandleFile(DocumentId documentId, IFileDescriptor descriptor)
        {
            return _formats.Contains(descriptor.FileNameWithExtension.Extension);
        }

        public override void Start(DocumentId documentId, IFileDescriptor descriptor)
        {
            JobHelper.QueueResize(
                Id, 
                documentId, 
                descriptor.FileId,
                descriptor.FileNameWithExtension.Extension
            );
        }

        public override void FormatAvailable(DocumentId documentId, DocumentFormat format, FileId fileId)
        {
            Logger.DebugFormat("{0}: new format available {1}", documentId, format);
        }
    }
}
