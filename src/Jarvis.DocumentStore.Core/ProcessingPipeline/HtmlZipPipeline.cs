using System.Linq;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.ProcessingPipeline
{
    public class HtmlZipPipeline: AbstractPipeline
    {
        readonly IJobHelper _jobHelper;
        public HtmlZipPipeline(IJobHelper jobHelper) :base("htmpzip")
        {
            _jobHelper = jobHelper;
        }

        public override bool ShouldHandleFile(DocumentId documentId, IFileDescriptor descriptor)
        {
            return descriptor.FileNameWithExtension.Extension == "htmlzip";
        }

        public override void Start(DocumentId documentId, IFileDescriptor descriptor)
        {
            _jobHelper.QueueHtmlToPdfConversion(Id, documentId, descriptor.FileId);
        }

        public override void FormatAvailable(DocumentId documentId, DocumentFormat format, FileId fileId)
        {

        }
    }
}