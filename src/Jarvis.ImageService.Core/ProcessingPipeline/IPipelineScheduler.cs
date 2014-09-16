using System.Collections.Generic;
using Jarvis.ImageService.Core.Model;

namespace Jarvis.ImageService.Core.ProcessingPipeline
{
    public interface IPipelineScheduler
    {
        void QueueThumbnail(string documentId, SizeInfo[] sizes);
    }
}