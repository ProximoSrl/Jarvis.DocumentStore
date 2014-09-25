using System.IO;
using System.IO.Compression;
using Castle.Core.Logging;
using MsgReader;

namespace Jarvis.DocumentStore.Core.ProcessingPipeline.Conversions
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

        public string Convert(string pathToEml, string workingFolder)
        {
            Logger.DebugFormat("Coverting {0} in working folder {1}", pathToEml, workingFolder);

            var reader = new Reader();
            var fname = Path.GetFileNameWithoutExtension(pathToEml);
            var outFolder = Path.Combine(workingFolder, fname);

            Logger.DebugFormat("Creating message working folder is {0}", outFolder);

            Directory.CreateDirectory(outFolder);

            Logger.Debug("Extracting files");
            reader.ExtractToFolder(pathToEml, outFolder);

            var pathToZip = Path.Combine(workingFolder, Path.ChangeExtension(fname, "htmlzip"));

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
