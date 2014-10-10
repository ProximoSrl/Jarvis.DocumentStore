using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public class AnalyzeEmailJob : AbstractFileJob
    {
        protected override void OnExecute(IJobExecutionContext context)
        {
            var task = new MailMessageToHtmlConverterTask()
            {
                Logger = Logger
            };

            var localFile = DownloadFileToWorkingFolder(this.FileId);
            var zipFile = task.Convert(this.FileId, localFile, WorkingFolder);

            var emailFileId = new FileId(this.FileId + ".ezip");
            FileStore.Upload(emailFileId, zipFile);

            CommandBus.Send(new AddFormatToDocument(
                this.DocumentId, 
                DocumentFormats.Email,
                emailFileId,
                PipelineId
            ));
        }
    }
}
