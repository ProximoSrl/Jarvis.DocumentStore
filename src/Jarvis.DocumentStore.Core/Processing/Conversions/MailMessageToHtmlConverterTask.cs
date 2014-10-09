using System.IO;
using System.IO.Compression;
using System.Linq;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Model;
using MsgReader;

namespace Jarvis.DocumentStore.Core.Processing.Conversions
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

        public string Convert(FileId fileId, string pathToEml, string workingFolder)
        {
            Logger.DebugFormat("Coverting {0} in working folder {1}", pathToEml, workingFolder);

            var reader = new Reader();
            var outFolder = Path.Combine(workingFolder, fileId);

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
            var htmlName = Path.GetFileNameWithoutExtension(files.First(x => x.ToLowerInvariant().EndsWith(".htm")));
            var pathToZip = Path.Combine(workingFolder, htmlName + ".email.zip");

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
    }
}
