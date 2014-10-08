using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ProcessingPipeline;
using Jarvis.DocumentStore.Core.ProcessingPipeline.Conversions;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public class HtmlToPdfJob : AbstractFileJob
    {
        public ConfigService ConfigService { get; set; }
        
        protected override void OnExecute(IJobExecutionContext context)
        {
            var converter = new HtmlToPdfConverter(FileStore, ConfigService)
            {
                Logger = Logger
            };

            var pdfId = converter.Run(this.FileId);
            CommandBus.Send(new AddFormatToDocument(
                this.DocumentId,
                new DocumentFormat(DocumentFormats.Pdf),
                pdfId,
                this.PipelineId
            ));
        }
    }
}
