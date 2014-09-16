using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.ImageService.Core.ProcessingPipeline;
using Jarvis.ImageService.Core.ProcessinPipeline;
using Jarvis.ImageService.Core.Storage;
using Quartz;

namespace Jarvis.ImageService.Core.Jobs
{
    public class CreateThumbnailFromPdfJob : IJob
    {
        public const string Documentid = "documentId";

        public ILogger Logger { get; set; }
        public IFileStore FileStore { get; set; }

        public void Execute(IJobExecutionContext context)
        {
            var documentId = context.JobDetail.JobDataMap.GetString(Documentid);

            var task = new CreatePdfImageTask();
            var fileReader = FileStore.OpenRead(documentId);
            using (var sourceStream = fileReader.OpenRead())
            {
                var convertParams = new CreatePdfImageTaskParams();
                task.Convert(sourceStream, convertParams, (i, stream) =>
                {
                    Logger.DebugFormat("Writing page {0}", i);
                    var fileId = documentId + "/thumbnail";
                    using (var destStream = FileStore.CreateNew(fileId,documentId+".png"))
                    {
                        stream.CopyTo(destStream);
                    }
                });
            }
        }
    }
}
