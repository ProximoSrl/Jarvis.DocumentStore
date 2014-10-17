using System.Linq;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.Processing.Pipeline
{
    public class OfficePipeline: AbstractPipeline
    {
        readonly IJobHelper _jobHelper;
        readonly string[] _formats;
        public OfficePipeline(IJobHelper jobHelper):base("office")
        {
            _jobHelper = jobHelper;
            _formats = "xls|xlsx|docx|doc|ppt|pptx|pps|ppsx|rtf|odt|ods|odp".Split('|');
        }

        public override bool ShouldHandleFile(DocumentId documentId, IFileDescriptor descriptor)
        {
            return _formats.Contains(descriptor.FileNameWithExtension.Extension);
        }

        protected override void OnStart(DocumentId documentId, IFileDescriptor descriptor)
        {
            _jobHelper.QueueLibreOfficeToPdfConversion(Id, documentId, descriptor.FileId);
        }

        protected override void OnFormatAvailable(DocumentId documentId, DocumentFormat format, FileId fileId)
        {
            if (format == DocumentFormats.Pdf)
            {
                PipelineManager.Start(documentId, fileId);
            }
        }
    }
}