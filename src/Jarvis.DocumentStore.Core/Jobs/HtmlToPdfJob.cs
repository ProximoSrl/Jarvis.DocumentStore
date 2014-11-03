using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public class HtmlToPdfJob : AbstractFileJob
    {
        protected override void OnExecute(IJobExecutionContext context)
        {
            var converter = new HtmlToPdfConverter(BlobStore, ConfigService)
            {
                Logger = Logger
            };

            var pdfId = converter.Run(TenantId, InputBlobId);
            CommandBus.Send(new AddFormatToDocument(
                this.InputDocumentId,
                new DocumentFormat(DocumentFormats.Pdf),
                pdfId,
                this.PipelineId
            ));
        }
    }
}
