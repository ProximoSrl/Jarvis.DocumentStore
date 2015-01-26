﻿using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormats = Jarvis.DocumentStore.Core.Processing.DocumentFormats;

namespace Jarvis.DocumentStore.Core.Jobs.OutOfProcessPollingJobs
{
    public class AnalyzeEmailOutOfProcessJob: AbstractOutOfProcessPollerFileJob
    {
        public AnalyzeEmailOutOfProcessJob()
        {
            base.PipelineId = new PipelineId("email");
            base.QueueName = "email";
        }


        protected async override Task<bool> OnPolling(PollingJobs.PollerJobParameters parameters, string workingFolder)
        {
            var task = new MailMessageToHtmlConverterTask()
            {
                Logger = Logger
            };

            string localFile = await DownloadBlob(
                parameters.TenantId, 
                parameters.InputBlobId, 
                parameters.FileExtension,
                workingFolder);

            var zipFile = task.Convert(parameters.InputBlobId, localFile, workingFolder);

            await AddFormatToDocumentFromFile(
              parameters.TenantId,
              parameters.InputDocumentId,
              new DocumentFormat(DocumentFormats.Email),
              zipFile,
              new Dictionary<string, object>());
            
            return true;
        }
    }
}