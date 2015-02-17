using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Jobs.OutOfProcessPollingJobs;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Shared.Jobs;
using Jarvis.DocumentStore.Shared.Model;

namespace Jarvis.DocumentStore.Jobs.Tika
{
    public abstract class AbstractTikaOutOfProcessJob : AbstractOutOfProcessPollerJob
    {
        readonly string[] _formats;

        public AbstractTikaOutOfProcessJob()
        {
            _formats = "pdf|xls|xlsx|docx|doc|ppt|pptx|pps|ppsx|rtf|odt|ods|odp|txt".Split('|');
            base.PipelineId = "tika";
            base.QueueName = "tika";
        }

        protected abstract ITikaAnalyzer BuildAnalyzer();

        protected override int ThreadNumber
        {
            get
            {
                return 6;
            }
        }

        protected async override Task<bool> OnPolling(
            PollerJobParameters parameters,
            String workingFolder)
        {
            Boolean result;
            if (!_formats.Contains(parameters.FileExtension))
            {
                Logger.DebugFormat("Document for job Id {0} has an extension not supported, setting null content", parameters.JobId);
                result = await AddFormatToDocumentFromObject(parameters.TenantId,
                    this.QueueName,
                    parameters.JobId,
                    new DocumentFormat(DocumentFormats.Content), DocumentContent.NullContent, new Dictionary<string, object>());
                return result;
            }

            Logger.DebugFormat("Starting tika on job: {0}, file extension {1}", parameters.JobId, parameters.FileExtension);
            var analyzer = BuildAnalyzer();
            Logger.DebugFormat("Downloading blob for job: {0}, on local path {1}", parameters.JobId, workingFolder);

            string pathToFile = await DownloadBlob(parameters.TenantId, parameters.JobId, parameters.FileExtension, workingFolder);
            string content = analyzer.GetHtmlContent(pathToFile) ?? "";
            Logger.DebugFormat("Finished tika on job: {0}, charsNum {1}", parameters.JobId, content.Count());

            var tikaFileName = Path.Combine(workingFolder, parameters.JobId + ".tika.html");
            File.WriteAllText(tikaFileName, content);
            result =  await AddFormatToDocumentFromFile(
                parameters.TenantId,
                parameters.JobId,
                new DocumentFormat(DocumentFormats.Tika), 
                tikaFileName, 
                new Dictionary<string, object>());
            Logger.DebugFormat("Added format {0} to jobId {1}, result: {2}", DocumentFormats.Tika, parameters.JobId, result);

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
                      this.QueueName,
                      parameters.JobId,
                      new DocumentFormat(DocumentFormats.Content),
                      documentContent,
                      new Dictionary<string, object>());
                Logger.DebugFormat("Added format {0} to jobId {1}, result: {2}", DocumentFormats.Content, parameters.JobId, result);
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
