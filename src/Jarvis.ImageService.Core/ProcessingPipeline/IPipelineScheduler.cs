using System.Collections.Generic;
using Jarvis.ImageService.Core.Model;

namespace Jarvis.ImageService.Core.ProcessingPipeline
{
    public interface IPipelineScheduler
    {
        void QueueThumbnail(ImageInfo imageInfo);
        void QueuePdfConversion(ImageInfo imageInfo);
    }
}