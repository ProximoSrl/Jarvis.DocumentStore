using System.Linq;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.Processing.Pipeline
{
    public class TikaPipeline: AbstractPipeline
    {
        readonly IJobHelper _jobHelper;
        readonly string[] _formats;
        public TikaPipeline(IJobHelper jobHelper) : base("tika")
        {
            _jobHelper = jobHelper;
            _formats = "pdf|xls|xlsx|docx|doc|ppt|pptx|pps|ppsx|rtf|odt|ods|odp".Split('|');
        }

        public override bool ShouldHandleFile(DocumentId documentId, IFileDescriptor descriptor)
        {
            return _formats.Contains(descriptor.FileNameWithExtension.Extension);
        }

        protected override void OnStart(DocumentId documentId, IFileDescriptor descriptor)
        {
            _jobHelper.QueueTikaAnalyzer(Id, documentId, descriptor.FileId);
        }

        protected override void OnFormatAvailable(DocumentId documentId, DocumentFormat format, FileId fileId)
        {
        
        }
    }
}