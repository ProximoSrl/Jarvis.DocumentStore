using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ProcessingPipeline.Conversions;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public class ExtractTextWithTikaJob : AbstractFileJob
    {
        protected override void OnExecute(IJobExecutionContext context)
        {
            var analyzer = new TikaAnalyzer(ConfigService)
            {
                Logger = this.Logger
            };

            string pathToFile = DownloadFile(this.FileId);

            analyzer.Run(pathToFile, content =>
            {
                var tikaFileId = new FileId(this.FileId + ".tika.html");
                using (var htmlReader = new MemoryStream(Encoding.UTF8.GetBytes(content)))
                {
                    FileStore.Upload(tikaFileId, tikaFileId, htmlReader);
                }

                Logger.DebugFormat("Tika result: file {1} has {0} chars", FileId, content.Length);
            });
        }
    }
}
