using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.ImageService.Core.Services;
using Jarvis.ImageService.Core.Storage;

namespace Jarvis.ImageService.Core.ProcessingPipeline.Conversions
{
    /// <summary>
    /// Office / OpenOffice => pdf with Headless Libreoffice
    /// TODO: switch to https://wiki.openoffice.org/wiki/AODL when complete pdf support is available
    /// </summary>
    public class ConvertToPdfTask
    {
        public ILogger Logger { get; set; }
        
        readonly IFileStore _fileStore;
        readonly ConfigService _config;

        public ConvertToPdfTask(IFileStore fileStore, ConfigService config)
        {
            _fileStore = fileStore;
            _config = config;
        }

        public void Convert(string fileId)
        {
            Logger.DebugFormat("Starting conversion of fileId {0} to pdf", fileId);
            string pathToLibreOffice = _config.GetPathToLibreOffice();
            var workingFolder = _config.GetWorkingFolder(fileId);

            var sourceFile = _fileStore.Download(fileId, workingFolder);
            var outputFile = Path.ChangeExtension(sourceFile, ".pdf");

            string arguments = string.Format("--headless -convert-to pdf -outdir \"{0}\"  \"{1}\" ",
                workingFolder,
                sourceFile
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

            _fileStore.Upload(fileId, outputFile);

            try
            {
                Logger.DebugFormat("Removing working folder {0}", workingFolder);
                Directory.Delete(workingFolder, true);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat(ex, "Unable to delete folder {0}", workingFolder);
            }
        }

        public bool CanHandle(string extension)
        {
            return true;
        }
    }
}
