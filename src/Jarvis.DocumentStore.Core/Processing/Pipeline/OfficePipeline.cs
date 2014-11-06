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

        public override bool ShouldHandleFile(DocumentId documentId, IBlobDescriptor storeDescriptor, IPipeline fromPipeline)
        {
            return _formats.Contains(storeDescriptor.FileNameWithExtension.Extension);
        }

        protected override void OnStart(DocumentId documentId, IBlobDescriptor storeDescriptor)
        {
            _jobHelper.QueueLibreOfficeToPdfConversion(Id, documentId, storeDescriptor.BlobId);
        }

        protected override void OnFormatAvailable(
            DocumentId documentId, 
            DocumentFormat format, 
            IBlobDescriptor descriptor
        ){
            if (format == DocumentFormats.Pdf)
            {
                PipelineManager.Start(documentId, descriptor, this);
            }
        }
    }
}