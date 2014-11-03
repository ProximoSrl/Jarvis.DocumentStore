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

            var localFile = DownloadFileToWorkingFolder(this.InputBlobId);
            var zipFile = task.Convert(this.InputBlobId, localFile, WorkingFolder);

            var blobId = BlobStore.Upload(DocumentFormats.Email, zipFile);

            CommandBus.Send(new AddFormatToDocument(
                this.InputDocumentId, 
                DocumentFormats.Email,
                blobId,
                PipelineId
            ));
        }
    }
}
