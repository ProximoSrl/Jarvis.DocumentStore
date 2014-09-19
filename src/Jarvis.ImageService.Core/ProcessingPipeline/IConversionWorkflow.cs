using System.Collections.Generic;
using Jarvis.ImageService.Core.Model;

namespace Jarvis.ImageService.Core.ProcessingPipeline
{
    public interface IConversionWorkflow
    {
        void QueueThumbnail(FileInfo fileInfo);
        void QueueLibreOfficeToPdfConversion(FileInfo fileInfo);
        void QueueHtmlToPdfConversion(FileInfo fileInfo);
        void QueueResize(FileInfo fileInfo);
        void Next(FileId fileId, string nextJob);
        void Start(FileId fileId);
    }
}