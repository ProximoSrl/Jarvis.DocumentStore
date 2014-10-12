using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Processing
{
    public interface IJobHelper
    {
        void QueueThumbnail(PipelineId pipelineId, DocumentId documentId, FileId fileId,string imageFormat);
        void QueueResize(PipelineId pipelineId, DocumentId documentId, FileId fileId, string imageFormat);
        void QueueLibreOfficeToPdfConversion(PipelineId pipelineId, DocumentId documentId, FileId fileId);
        void QueueHtmlToPdfConversion(PipelineId pipelineId, DocumentId documentId, FileId fileId);
        void QueueTikaAnalyzer(PipelineId pipelineId, DocumentId documentId, FileId fileId);
        void QueueEmailToHtml(PipelineId pipelineId, DocumentId documentId, FileId fileId);
        void QueueCommand(ICommand command, string asUser);
    }
}