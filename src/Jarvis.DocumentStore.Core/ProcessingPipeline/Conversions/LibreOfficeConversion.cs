using System;
using System.Diagnostics;
using System.IO;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;

namespace Jarvis.DocumentStore.Core.ProcessingPipeline.Conversions
{
    /// <summary>
    /// Office / OpenOffice => pdf with Headless Libreoffice
    /// TODO: switch to https://wiki.openoffice.org/wiki/AODL when complete pdf support is available
    /// </summary>
    public class LibreOfficeConversion
    {
        public ILogger Logger { get; set; }
        
        readonly IFileStore _fileStore;
        readonly ConfigService _config;

        public LibreOfficeConversion(IFileStore fileStore, ConfigService config)
        {
            _fileStore = fileStore;
            _config = config;
        }

        public FileId Run(FileId fileId, string outType)
        {
            Logger.DebugFormat("Starting conversion of fileId {0} to {1}", fileId, outType);
            string pathToLibreOffice = _config.GetPathToLibreOffice();
            var workingFolder = _config.GetWorkingFolder(fileId);

            var sourceFile = _fileStore.Download(fileId, workingFolder);
            var outputFile = Path.ChangeExtension(sourceFile, outType);

            string arguments = string.Format("--headless -convert-to {2} -outdir \"{0}\"  \"{1}\" ",
                workingFolder,
                sourceFile,
                outType
            );

            var psi = new ProcessStartInfo(pathToLibreOffice, arguments)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Minimized
            };

            Logger.DebugFormat("Command: {0} {1}", pathToLibreOffice, arguments);

            using (var p = Process.Start(psi))
            {
                Logger.Debug("Process started");
                p.WaitForExit();
                Logger.Debug("Process ended");
            }

            if(!File.Exists(outputFile))
                throw new Exception("Conversion failed");

            var newFileId = new FileId(fileId + "." + outType);
            _fileStore.Upload(newFileId, outputFile);

            try
            {
                Logger.DebugFormat("Removing working folder {0}", workingFolder);
                Directory.Delete(workingFolder, true);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat(ex, "Unable to delete folder {0}", workingFolder);
            }

            return newFileId;
        }
    }
}
