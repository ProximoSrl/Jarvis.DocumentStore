using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.JobsHost.Helpers;
using Jarvis.DocumentStore.Shared.Jobs;
using Jarvis.DocumentStore.Shared.Model;
using Jarvis.DocumentStore.Jobs.Tika.Filters;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using System.IO.Compression;
using Jarvis.DocumentStore.Jobs.HtmlZipOld;
using System.Configuration;

namespace Jarvis.DocumentStore.Jobs.Tika
{
    public abstract class AbstractTikaOutOfProcessJob : AbstractOutOfProcessPollerJob
    {
        private readonly string[] _formats;

        private readonly ContentFormatBuilder _builder;

        private readonly ContentFilterManager _filterManager;
        private readonly Int32 _threadNumber;

        public AbstractTikaOutOfProcessJob(
            ContentFormatBuilder builder,
            ContentFilterManager filterManager)
        {
            _builder = builder;
            _filterManager = filterManager;
            _formats = "pdf|xls|xlsx|docx|doc|ppt|pptx|pps|ppsx|rtf|odt|ods|odp|txt|tmp|htmlzip|htmzip|mht|mhtml|eml|htm|html".Split('|');
            base.PipelineId = "tika";
            base.QueueName = "tika";

            var config = ConfigurationManager.AppSettings["threadNumber"] ?? "1";
            if (!Int32.TryParse(config, out _threadNumber))
            {
                _threadNumber = 1;
            }
        }

        /// <summary>
        /// the process can create a list of analyzer, because it could happen that one analyzer
        /// failed analysis and then he need to try with the next one.
        /// </summary>
        /// <param name="analyzerProgressive"></param>
        /// <returns></returns>
        protected abstract ITikaAnalyzer BuildAnalyzer(Int32 analyzerProgressive);

        protected override int ThreadNumber => _threadNumber;

        protected async override Task<ProcessResult> OnPolling(
            PollerJobParameters parameters,
            String workingFolder)
        {
            Boolean result;
            var contentFileName = Path.ChangeExtension(parameters.FileName, ".content");
            if (!_formats.Contains(parameters.FileExtension))
            {
                Logger.DebugFormat("Document for job Id {0} has an extension not supported, setting null content", parameters.JobId);
                return new ProcessResult(await AddNullContentFormat(parameters, contentFileName));
            }

            Logger.DebugFormat("Starting tika on job: {0}, file extension {1}", parameters.JobId, parameters.FileExtension);

            Logger.DebugFormat("Downloading blob for job: {0}, on local path {1}", parameters.JobId, workingFolder);
            string pathToFile = await DownloadBlob(parameters.TenantId, parameters.JobId, parameters.FileName, workingFolder);

            pathToFile = ProcessFile(pathToFile, workingFolder);

            Boolean shouldAnalyze = _filterManager.ShouldAnalyze(parameters.FileName, pathToFile);
            if (!shouldAnalyze)
            {
                Logger.InfoFormat("File {0} for job {1} was discharded!", parameters.FileName, parameters.JobId);
                return new ProcessResult(await AddNullContentFormat(parameters, contentFileName));
            }
            Logger.DebugFormat("Search for password JobId:{0}", parameters.JobId);
            var passwords = ClientPasswordSet.GetPasswordFor(parameters.FileName).ToArray();
            String content = "";
            Int32 analyzerOrdinal = 0;
            Boolean success = false;

            var analyzer = BuildAnalyzer(analyzerOrdinal);
            do
            {
                try
                {
                    if (passwords.Any())
                    {
                        //Try with all the password
                        foreach (var password in passwords)
                        {
                            try
                            {
                                content = analyzer.GetHtmlContent(pathToFile, password) ?? "";
                                break; //first password that can decrypt file break the list of password to try
                            }
                            catch (Exception)
                            {
                                Logger.ErrorFormat("Error opening file {0} with password", parameters.FileName);
                            }
                        }
                    }
                    else
                    {
                        //Simply analyze file without password
                        Logger.DebugFormat("Analyze content JobId: {0} -> Path: {1}", parameters.JobId, pathToFile);
                        content = analyzer.GetHtmlContent(pathToFile, "") ?? "";
                    }
                    success = true;
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat(ex, "Error extracting tika with analyzer {0} on file {1}", analyzer.Describe(), parameters.FileName, parameters.JobId);
                    analyzer = BuildAnalyzer(++analyzerOrdinal);
                    if (analyzer != null) Logger.InfoFormat("Retry job  {0} with analyzer {1}", parameters.JobId, analyzer.Describe());
                }
            } while (analyzer != null && success == false);

            if (!success) 
            {
                return ProcessResult.Fail("Unable to extract content with any version of tika present on the machine.");
            }

            Logger.DebugFormat("Finished tika on job: {0}, charsNum {1}", parameters.JobId, content.Count());
            String sanitizedContent = content;
            if (!string.IsNullOrWhiteSpace(content))
            {
                var resultContent = _builder.CreateFromTikaPlain(content);
                var documentContent = resultContent.Content;
                sanitizedContent = resultContent.SanitizedTikaContent;
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
                      contentFileName,
                      new Dictionary<string, object>());
                Logger.DebugFormat("Added format {0} to jobId {1}, result: {2}", DocumentFormats.Content, parameters.JobId, result);
            }

            var tikaFileName = Path.Combine(workingFolder, Path.GetFileNameWithoutExtension(parameters.FileName) + ".tika.html");
            tikaFileName = SanitizeFileNameForLength(tikaFileName);
            File.WriteAllText(tikaFileName, sanitizedContent);
            result = await AddFormatToDocumentFromFile(
                parameters.TenantId,
                parameters.JobId,
                new DocumentFormat(DocumentFormats.Tika),
                tikaFileName,
                new Dictionary<string, object>());
            Logger.DebugFormat("Added format {0} to jobId {1}, result: {2}", DocumentFormats.Tika, parameters.JobId, result);

            return ProcessResult.Ok;
        }

        private string ProcessFile(string pathToFile, string workingFolder)
        {
            var extension = Path.GetExtension(pathToFile).ToLower();
            if (extension == ".htmlzip" || extension == ".htmzip")
            {
                ZipFile.ExtractToDirectory(pathToFile, workingFolder);
                Logger.DebugFormat("Extracted zip to {0}", workingFolder);

                var htmlFile = Path.ChangeExtension(pathToFile, "html");
                if (File.Exists(htmlFile))
                {
                    Logger.DebugFormat("Html file is {0}", htmlFile);
                    return htmlFile;
                }

                htmlFile = Path.ChangeExtension(pathToFile, "htm");
                if (File.Exists(htmlFile))
                {
                    Logger.DebugFormat("Html file is {0}", htmlFile);
                    return htmlFile;
                }

                Logger.ErrorFormat("Invalid HTMLZIP file, name is {0} but corresponding html file not found after decompression", Path.GetFileName(pathToFile));
            }
            else if (extension == ".mht" || extension == ".mhtml")
            {
                MHTMLParser parser = new MHTMLParser(File.ReadAllText(pathToFile));
                parser.OutputDirectory = workingFolder;
                parser.DecodeImageData = false;
                var html = parser.getHTMLText();
                pathToFile = pathToFile + ".html";
                File.WriteAllText(pathToFile, html);
            }
            return pathToFile;
        }

        private async Task<bool> AddNullContentFormat(
            PollerJobParameters parameters, string contentFileName)
        {
            return await AddFormatToDocumentFromObject(parameters.TenantId,
                this.QueueName,
                parameters.JobId,
                new DocumentFormat(DocumentFormats.Content),
                DocumentContent.NullContent,
                contentFileName,
                new Dictionary<string, object>());
        }
    }
}
