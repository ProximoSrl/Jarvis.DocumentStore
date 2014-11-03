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

        public override bool ShouldHandleFile(DocumentId documentId, IFileStoreDescriptor storeDescriptor)
        {
            return _formats.Contains(storeDescriptor.FileNameWithExtension.Extension);
        }

        protected override void OnStart(DocumentId documentId, IFileStoreDescriptor storeDescriptor)
        {
            _jobHelper.QueueTikaAnalyzer(Id, documentId, storeDescriptor.BlobId);
        }

        protected override void OnFormatAvailable(DocumentId documentId, DocumentFormat format, IFileStoreDescriptor descriptor)
        {
        }
    }
}