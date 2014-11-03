using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.Processing.Pipeline
{
    public class HtmlZipPipeline: AbstractPipeline
    {
        readonly IJobHelper _jobHelper;
        public HtmlZipPipeline(IJobHelper jobHelper) :base("htmlzip")
        {
            _jobHelper = jobHelper;
        }

        public override bool ShouldHandleFile(
            DocumentId documentId, 
            IFileStoreDescriptor storeDescriptor
        )
        {
            if (storeDescriptor.FileNameWithExtension.Extension == "htmlzip")
                return true;

            if (storeDescriptor.FileNameWithExtension.Extension == "ezip")
                return true;

            return false;
        }

        protected override void OnStart(DocumentId documentId, IFileStoreDescriptor storeDescriptor)
        {
            _jobHelper.QueueHtmlToPdfConversion(Id, documentId, storeDescriptor.FileId);
        }

        protected override void OnFormatAvailable(DocumentId documentId, DocumentFormat format, IFileStoreDescriptor descriptor)
        {
            PipelineManager.Start(documentId, descriptor);
        }
    }
}