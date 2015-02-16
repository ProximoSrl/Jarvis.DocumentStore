using System.Collections.Generic;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Jobs.OutOfProcessPollingJobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.JobsHost.Processing.Conversions;
using DocumentFormats = Jarvis.DocumentStore.Core.Processing.DocumentFormats;

namespace Jarvis.DocumentStore.JobsHost.Jobs
{
    public class AnalyzeEmailOutOfProcessJob: AbstractOutOfProcessPollerFileJob
    {
        public AnalyzeEmailOutOfProcessJob()
        {
            base.PipelineId = new PipelineId("email");
            base.QueueName = "email";
        }


        protected async override Task<bool> OnPolling(PollerJobParameters parameters, string workingFolder)
        {
            var task = new MailMessageToHtmlConverterTask()
            {
                Logger = Logger
            };

            string localFile = await DownloadBlob(
                parameters.TenantId, 
                parameters.JobId, 
                parameters.FileExtension,
                workingFolder);

            var zipFile = task.Convert(parameters.JobId, localFile, workingFolder);

            await AddFormatToDocumentFromFile(
              parameters.TenantId,
              parameters.JobId,
              new DocumentFormat(DocumentFormats.Email),
              zipFile,
              new Dictionary<string, object>());
            
            return true;
        }
    }
}
