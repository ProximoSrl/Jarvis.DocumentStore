using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Processing
{
    public interface IJobHelper
    {
        void QueueThumbnail(PipelineId pipelineId, DocumentId documentId, BlobId blobId,string imageFormat);
        void QueueResize(PipelineId pipelineId, DocumentId documentId, BlobId blobId, string imageFormat);
        void QueueLibreOfficeToPdfConversion(PipelineId pipelineId, DocumentId documentId, BlobId blobId);
        void QueueHtmlToPdfConversion(PipelineId pipelineId, DocumentId documentId, BlobId blobId);
        void QueueTikaAnalyzer(PipelineId pipelineId, DocumentId documentId, BlobId blobId);
        void QueueEmailToHtml(PipelineId pipelineId, DocumentId documentId, BlobId blobId);
    }
}