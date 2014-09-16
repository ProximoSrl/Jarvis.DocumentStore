using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private static Regex _regex = new Regex("([a-z]+):([0-9]+)x([0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public const string DocumentIdKey = "documentId";
        public const string Sizes = "sizes";
        
        private string DocumentId { get; set; }

        public ILogger Logger { get; set; }
        public IFileStore FileStore { get; set; }

        public void Execute(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            DocumentId = jobDataMap.GetString(DocumentIdKey);

            var task = new CreatePdfImageTask();
            var descriptor = FileStore.GetDescriptor(DocumentId);
            using (var sourceStream = descriptor.OpenRead())
            {
                var convertParams = new CreatePdfImageTaskParams()
                {
                    Dpi = jobDataMap.GetIntOrDefault("dpi", 150),
                    FromPage = jobDataMap.GetIntOrDefault("pages.from", 1),
                    Pages = jobDataMap.GetIntOrDefault("pages.count", 1)
                };

                task.Convert(sourceStream, convertParams, SaveRasterizedPage);
            }

            Logger.DebugFormat("Deleting document {0}", DocumentId);

            FileStore.Delete(DocumentId);
            Logger.Debug("Task completed");
        }

        void SaveRasterizedPage(int i, Stream pageStream)
        {
            foreach (var size in ParseResize("small:200x200|large:800x800"))
            {
                pageStream.Seek(0, SeekOrigin.Begin);
                var resizeId = DocumentId + "/thumbnail/" + size.Item1;
                Logger.DebugFormat("Writing page {0} - {1}", i, resizeId);
                using (var destStream = FileStore.CreateNew(resizeId, DocumentId + "." + size.Item1 + ".png"))
                {
                    ImageResizer.Shrink(pageStream, destStream, size.Item2, size.Item3);
                }
            }
        }

        Tuple<string, int, int>[] ParseResize(string sizes)
        {
            return (from s in sizes.Split('|')
                    let m = _regex.Match(s)
                    where m.Success
                    select new Tuple<string, int, int>(
                        m.Groups[1].Value,
                        int.Parse(m.Groups[2].Value),
                        int.Parse(m.Groups[3].Value)
                        )).ToArray();
        }
    }
}