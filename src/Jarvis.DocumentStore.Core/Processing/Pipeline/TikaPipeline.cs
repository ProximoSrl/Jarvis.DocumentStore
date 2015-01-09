using System.Linq;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.Processing.Pipeline
{
    public class TikaPipeline: AbstractPipeline
    {
        readonly IJobHelper _jobHelper;
        public TikaPipeline(IJobHelper jobHelper) : base("tika")
        {
            _jobHelper = jobHelper;
        }

        public override bool ShouldHandleFile(DocumentId documentId, IBlobDescriptor storeDescriptor, IPipeline fromPipeline)
        {
            if (fromPipeline != null && fromPipeline.Id == "office")
                return false;
            return true;
        }

        protected override void OnStart(DocumentId documentId, IBlobDescriptor storeDescriptor)
        {
            _jobHelper.QueueTikaAnalyzer(Id, documentId, storeDescriptor.BlobId, storeDescriptor.FileNameWithExtension.Extension);
        }

        protected override void OnFormatAvailable(DocumentId documentId, DocumentFormat format, IBlobDescriptor descriptor)
        {
        }
    }
}