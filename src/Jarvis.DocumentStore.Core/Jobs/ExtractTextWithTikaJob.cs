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

            string pathToFile = DownloadFileToWorkingFolder(this.FileId);

            analyzer.Run(pathToFile, content =>
            {
                var tikaFileName = new FileNameWithExtension(this.FileId + ".tika.html");
                FileId tikaFileId;
                using (var htmlReader = new MemoryStream(Encoding.UTF8.GetBytes(content)))
                {
                    tikaFileId = FileStore.Upload(tikaFileName, htmlReader);
                }

                CommandBus.Send(new AddFormatToDocument(
                    this.DocumentId,
                    new DocumentFormat("tika"),
                    tikaFileId,
                    this.PipelineId
                ));

                Logger.DebugFormat("Tika result: file {0} has {1} chars", FileId, content.Length);
            });
        }
    }
}
