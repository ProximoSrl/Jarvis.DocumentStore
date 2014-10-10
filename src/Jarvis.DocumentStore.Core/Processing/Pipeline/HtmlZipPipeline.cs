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

        public override bool ShouldHandleFile(DocumentId documentId, IFileDescriptor descriptor)
        {
            if (descriptor.FileNameWithExtension.Extension == "htmlzip")
                return true;

            if (descriptor.FileNameWithExtension.Extension == "ezip")
                return true;

            return false;
        }

        public override void Start(DocumentId documentId, IFileDescriptor descriptor)
        {
            _jobHelper.QueueHtmlToPdfConversion(Id, documentId, descriptor.FileId);
        }

        public override void FormatAvailable(DocumentId documentId, DocumentFormat format, FileId fileId)
        {
            PipelineManager.Start(documentId, fileId);
        }
    }
}