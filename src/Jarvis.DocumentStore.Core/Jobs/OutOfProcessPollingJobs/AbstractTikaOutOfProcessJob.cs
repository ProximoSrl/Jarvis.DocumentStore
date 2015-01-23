using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using com.sun.corba.se.spi.orbutil.threadpool;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Jobs.PollingJobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Processing.Analyzers;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Shared.Model;
using System.Linq;
using DocumentFormats = Jarvis.DocumentStore.Core.Processing.DocumentFormats;

namespace Jarvis.DocumentStore.Core.Jobs.OutOfProcessPollingJobs
{
    public abstract class AbstractTikaOutOfProcessJob : AbstractOutOfProcessPollerFileJob
    {
        readonly string[] _formats;

        public AbstractTikaOutOfProcessJob()
        {
            _formats = "pdf|xls|xlsx|docx|doc|ppt|pptx|pps|ppsx|rtf|odt|ods|odp|txt".Split('|');
            base.PipelineId = new PipelineId("tika");
            base.QueueName = "tika";
        }

        protected abstract ITikaAnalyzer BuildAnalyzer();

        protected async override Task<Boolean> OnPolling(
            PollerJobParameters parameters,
            String workingFolder)
        {
            Boolean result;
            if (!_formats.Contains(parameters.FileExtension))
            {
                Logger.DebugFormat("Document Id {0} has an extension not supported, setting null content", parameters.InputDocumentId);
                result = await AddFormatToDocumentFromObject(parameters.TenantId, parameters.InputDocumentId,
                    new DocumentFormat(DocumentFormats.Content), DocumentContent.NullContent, new Dictionary<string, object>());
                return result;
            }

            Logger.DebugFormat("Starting tika on content: {0}, file extension {1}", parameters.InputDocumentId, parameters.FileExtension);
            var analyzer = BuildAnalyzer();
            Logger.DebugFormat("Downloading blob id: {0}, on local path {1}", parameters.InputBlobId, workingFolder);

            string pathToFile = await DownloadBlob(parameters.TenantId, parameters.InputBlobId, parameters.FileExtension, workingFolder);
            string content = analyzer.GetHtmlContent(pathToFile) ?? "";
            Logger.DebugFormat("Finished tika on content: {0}, charsNum {1}", parameters.InputDocumentId, content.Count());

            var tikaFileName = Path.Combine(workingFolder, parameters.InputBlobId + ".tika.html");
            File.WriteAllText(tikaFileName, content);
            result =  await AddFormatToDocumentFromFile(
                parameters.TenantId, 
                parameters.InputDocumentId, 
                new DocumentFormat(DocumentFormats.Tika), 
                tikaFileName, 
                new Dictionary<string, object>());
            Logger.DebugFormat("Added format {0} to document {1}, result: {2}", DocumentFormats.Tika, parameters.InputDocumentId, result);

            if (!string.IsNullOrWhiteSpace(content))
            {
                var documentContent = ContentFormatBuilder.CreateFromTikaPlain(content);
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

                result = await AddFormatToDocumentFromObject(
                      parameters.TenantId,
                      parameters.InputDocumentId,
                      new DocumentFormat(DocumentFormats.Content),
                      documentContent,
                      new Dictionary<string, object>());
                Logger.DebugFormat("Added format {0} to document {1}, result: {2}", DocumentFormats.Content, parameters.InputDocumentId, result);
            }
            return true;
        }

    }

    public class OutOfProcessTikaJob : AbstractTikaOutOfProcessJob
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

    public class OutOfProcessTikaNetJob : AbstractTikaOutOfProcessJob
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
