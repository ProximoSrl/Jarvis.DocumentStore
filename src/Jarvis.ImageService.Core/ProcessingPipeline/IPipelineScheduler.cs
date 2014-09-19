using System.Collections.Generic;
using Jarvis.ImageService.Core.Model;

namespace Jarvis.ImageService.Core.ProcessingPipeline
{
    public interface IPipelineScheduler
    {
        void QueueThumbnail(FileInfo fileInfo);
        void QueuePdfConversion(FileInfo fileInfo);
        void QueueHtmlToPdfConversion(FileInfo fileInfo);
        void QueueResize(FileInfo fileInfo);
    }
}