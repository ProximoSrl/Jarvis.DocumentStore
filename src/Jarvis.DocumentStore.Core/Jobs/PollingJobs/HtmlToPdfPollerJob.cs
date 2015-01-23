using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using Jarvis.DocumentStore.Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Jobs.PollingJobs
{
    public class HtmlToPdfPollerJob : AbstractInProcessPollerFileJob
    {
        public HtmlToPdfPollerJob()
        {
            base.PipelineId = new PipelineId("htmlzip");
            base.QueueName = "htmlzip";
        }
        protected override void OnPolling(PollerJobParameters parameters, IBlobStore currentTenantBlobStore, string workingFolder)
        {
            var converter = new HtmlToPdfConverter(currentTenantBlobStore, ConfigService)
            {
                Logger = Logger
            };

            var pdfId = converter.Run(parameters.TenantId, parameters.InputBlobId);
            CommandBus.Send(new AddFormatToDocument(
                parameters.InputDocumentId,
                new DocumentFormat(DocumentFormats.Pdf),
                pdfId,
                this.PipelineId
            ));
        }
    }
}
