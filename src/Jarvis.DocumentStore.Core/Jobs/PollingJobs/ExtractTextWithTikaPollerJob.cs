using System.IO;
using System.Linq;
using System.Text;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Processing.Analyzers;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Shared.Model;
using System.Collections.Generic;
using System;
using Jarvis.DocumentStore.Core.Support;

namespace Jarvis.DocumentStore.Core.Jobs.PollingJobs
{
    public abstract class AbstractTikaPollerBaseJob : AbstractPollerFileJob
    {
        readonly string[] _formats;

        public AbstractTikaPollerBaseJob()
        {
            _formats = "pdf|xls|xlsx|docx|doc|ppt|pptx|pps|ppsx|rtf|odt|ods|odp|txt".Split('|');
            base.PipelineId = new PipelineId("tika");
            base.QueueName = "tika";
        }

        protected abstract ITikaAnalyzer BuildAnalyzer();

        protected override void OnPolling(
            PollerJobBaseParameters baseParameters, 
            IDictionary<String, String> fullParameters,
            IBlobStore currentTenantBlobStore,
            String workingFolder)
        {
            
            if (!_formats.Contains(baseParameters.FileExtension))
            {
                var contentId = currentTenantBlobStore.Save(DocumentFormats.Content, DocumentContent.NullContent);
                Logger.DebugFormat("Content: {0} has null content.", baseParameters.InputDocumentId);

                CommandBus.Send(new AddFormatToDocument(
                    baseParameters.InputDocumentId,
                    DocumentFormats.Content,
                    contentId,
                    this.PipelineId
                ));
                return;
            }
            Logger.DebugFormat("Starting tika on content: {0}, file extension {1}", baseParameters.InputDocumentId, baseParameters.FileExtension);
            var analyzer = BuildAnalyzer();

            string pathToFile = currentTenantBlobStore.Download(baseParameters.InputBlobId, workingFolder);
            string content = analyzer.GetHtmlContent(pathToFile);
            var tikaFileName = new FileNameWithExtension(baseParameters.InputBlobId + ".tika.html");
            BlobId tikaBlobId;
            string htmlSource = null;
            using (var htmlReader = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                tikaBlobId = currentTenantBlobStore.Upload(DocumentFormats.Tika, tikaFileName, htmlReader);
                htmlReader.Seek(0, SeekOrigin.Begin);
                using (var sr = new StreamReader(htmlReader, Encoding.UTF8))
                {
                    htmlSource = sr.ReadToEnd();
                }
            }

            Logger.DebugFormat("Tika result: file {0} has {1} chars", baseParameters.InputBlobId, content.Length);
            CommandBus.Send(new AddFormatToDocument(
                baseParameters.InputDocumentId,
                DocumentFormats.Tika,
                tikaBlobId,
                this.PipelineId
                ));

            if (!string.IsNullOrWhiteSpace(htmlSource))
            {
                var documentContent = ContentFormatBuilder.CreateFromTikaPlain(htmlSource);
                var pages = documentContent.Pages.Count();
                string lang = null;
                if (pages > 1)
                {
                    lang = LanguageDetector.GetLanguage(documentContent.Pages[1].Content);
                }

                if (lang == null && pages == 1)
                {
                    lang = LanguageDetector.GetLanguage(documentContent.Pages[0].Content);
                }

                if (lang != null)
                {
                    documentContent.AddMetadata(DocumentContent.MedatataLanguage, lang);
                }

                var contentId = currentTenantBlobStore.Save(DocumentFormats.Content, documentContent);
                Logger.DebugFormat("Content: {0} has {1} pages", baseParameters.InputDocumentId, pages);

                CommandBus.Send(new AddFormatToDocument(
                    baseParameters.InputDocumentId,
                    DocumentFormats.Content,
                    contentId,
                    this.PipelineId
                ));
            }
        }
    }

    public class ExtractTextWithTikaJob : AbstractTikaPollerBaseJob
    {

        protected override ITikaAnalyzer BuildAnalyzer()
        {
            return new TikaAnalyzer(ConfigService)
            {
                Logger = this.Logger
            };
        }

        public override bool IsActive
        {
            get { return !base.ConfigService.UseEmbeddedTika; }
        }
    }

    public class ExtractTextWithTikaNetJob : AbstractTikaPollerBaseJob
    {
        protected override ITikaAnalyzer BuildAnalyzer()
        {
            return new TikaNetAnalyzer();
        }

        public override bool IsActive
        {
            get { return base.ConfigService.UseEmbeddedTika; }
        }
    }
}
