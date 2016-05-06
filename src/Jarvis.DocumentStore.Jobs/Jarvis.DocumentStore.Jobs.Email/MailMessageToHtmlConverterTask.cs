using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Castle.Core.Logging;
using MsgReader;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using System.Text.RegularExpressions;

namespace Jarvis.DocumentStore.Jobs.Email
{
    /// <summary>
    /// Outlook email & msg conversion
    /// </summary>
    public class MailMessageToHtmlConverterTask
    {
        ILogger _logger = NullLogger.Instance;

        public ILogger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }

        public string Convert(String jobId, string pathToEml, string workingFolder)
        {
            Logger.DebugFormat("Coverting {0} in working folder {1}", pathToEml, workingFolder);

            var reader = new Reader();

            var outFolder = Path.Combine(workingFolder, jobId);

            Logger.DebugFormat("Creating message working folder is {0}", outFolder);

            Directory.CreateDirectory(outFolder);

            Logger.Debug("Extracting files");
 
            var files = reader.ExtractToFolder(pathToEml, outFolder);

            if (Logger.IsDebugEnabled)
            {
                foreach (var file in files)
                {
                    Logger.DebugFormat("\t{0}", Path.GetFileName(file));
                }
                Logger.DebugFormat("Total files {0}", files.Length);
            }
            var htmlFileName = files.FirstOrDefault(x => x.ToLowerInvariant().EndsWith(".htm")) ??
                files.FirstOrDefault(x => x.ToLowerInvariant().EndsWith(".html"));
            if (htmlFileName == null)
            {
                var textFile = files.FirstOrDefault(x => x.ToLowerInvariant().EndsWith(".txt"));
                if (textFile != null)
                {
                    htmlFileName = textFile + ".html";
                    var textcontent = File.ReadAllText(textFile);
                    File.WriteAllText(htmlFileName, String.Format("<html><body><pre>{0}</html></body></pre>", textcontent));
                }
                else
                {
                    htmlFileName = "contentmissing.html";
                    File.WriteAllText(htmlFileName, "<html>No content found in mail.</html>");
                }
            }
            var htmlNameWithoutExtension = Path.GetFileNameWithoutExtension(htmlFileName);

            var htmlContent = File.ReadAllText(htmlFileName);
            var dirInfoFullName = new DirectoryInfo(outFolder).FullName;
            htmlContent = Regex.Replace(
                htmlContent, 
                @"src=""(?<src>.+?)""", 
                new MatchEvaluator((m) => NormalizeImgEvaluator(m, dirInfoFullName)), 
                RegexOptions.IgnoreCase);
            File.WriteAllText(htmlFileName, htmlContent);

            var pathToZip = Path.Combine(workingFolder, htmlNameWithoutExtension + ".ezip");

            Logger.DebugFormat("New zip file is {0}", pathToZip);

            if (File.Exists(pathToZip)) {
                Logger.DebugFormat("Deleting previous file: {0}", pathToZip);
                File.Delete(pathToZip);
            }

            Logger.DebugFormat("Creating new file: {0}", pathToZip);
            ZipFile.CreateFromDirectory(outFolder, pathToZip);

            Logger.DebugFormat("Deleting message working folder", outFolder);
            Directory.Delete(outFolder, true);

            Logger.DebugFormat(
                "Convesion done {0} => {1}",
                pathToEml,
                pathToZip
            );
            return pathToZip;
        }

        private string NormalizeImgEvaluator(Match match, String directoryPrefix)
        {
            var imgGroup = match.Groups["src"].Value.ToLower();
            if (imgGroup.StartsWith(directoryPrefix.ToLower()))
            {
                imgGroup = imgGroup.Substring(directoryPrefix.Length).Trim('/', '\\');
            }
            return String.Format("src=\"{0}\"", imgGroup);
        }
    }
}
