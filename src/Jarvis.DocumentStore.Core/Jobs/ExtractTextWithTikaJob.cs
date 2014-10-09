using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Shared.Commands;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing.Conversions;
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
                    FileStore.Upload(tikaFileId, new FileNameWithExtension(tikaFileId), htmlReader);
                }

                CommandBus.Send(new AddFormatToDocument(
                    this.DocumentId,
                    new DocumentFormat("tika"),
                    tikaFileId,
                    this.PipelineId
                ));

                Logger.DebugFormat("Tika result: file {1} has {0} chars", FileId, content.Length);
            });
        }
    }
}
