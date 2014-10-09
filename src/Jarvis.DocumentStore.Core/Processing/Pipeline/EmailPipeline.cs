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

        public override bool ShouldHandleFile(DocumentId documentId, IFileDescriptor descriptor)
        {
            return _formats.Contains(descriptor.FileNameWithExtension.Extension);
        }

        public override void Start(DocumentId documentId, IFileDescriptor descriptor)
        {
            Logger.DebugFormat("Processing email {0}", descriptor.FileNameWithExtension);
            JobHelper.QueueEmailToHtml(Id, documentId, descriptor.FileId);
        }

        public override void FormatAvailable(DocumentId documentId, DocumentFormat format, FileId fileId)
        {
            Logger.DebugFormat("Email {0} has been converted to format {1}: {2}", documentId, format, fileId);
        }
    }
}
