using Castle.Core.Logging;
using Jarvis.DocumentStore.JobsHost.Support;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Linq;

namespace Jarvis.DocumentStore.Jobs.MsOffice
{
#pragma warning disable S2583 // Conditionally executed blocks should be reachable
#pragma warning disable S1854 // Dead stores should be removed
    public class PowerPointConverter
    {
        private readonly ILogger _logger;
        private readonly IClientPasswordSet _clientPasswordSet;

        public PowerPointConverter(ILogger logger, IClientPasswordSet clientPasswordSet)
        {
            _logger = logger;
            _clientPasswordSet = clientPasswordSet;
        }

        public string GetPassword(String fileName)
        {
            return _clientPasswordSet.GetPasswordFor(fileName).FirstOrDefault() ?? "fake password, to avoid being stuck with ask password dialog";
        }

        /// <summary>
        /// Convert power point to pdf
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="targetPath"></param>
        /// <returns>Error message, string empty if succeeded</returns>
        internal String ConvertToPdf(string sourcePath, string targetPath)
        {
            Application app = null;
            Presentation presentation = null;
            OfficeUtils.KillOfficeProcess("POWERPNT");
            try
            {
                app = new Application();
                //app.Visible = MsoTriState.msoFalse;
                //app.WindowState = PpWindowState.ppWindowMinimized;
                presentation = app.Presentations.Open(
                    sourcePath,
                    MsoTriState.msoFalse,
                    MsoTriState.msoFalse,
                    MsoTriState.msoFalse);

                presentation.ExportAsFixedFormat(
                    targetPath,
                    PpFixedFormatType.ppFixedFormatTypePDF,
                    PpFixedFormatIntent.ppFixedFormatIntentScreen);

                presentation.Close();
                presentation = null;
                app.Quit();
                app = null;

                return String.Empty;
            }
            catch (Exception ex)
            {
                _logger.ErrorFormat(ex, "Error converting {0} - {1}", sourcePath, ex.Message);

                if (presentation != null)
                {
                    this.Close(presentation);
                }
                if (app != null)
                {
                    this.Close(app);
                }
                return $"Error converting {sourcePath} - {ex.Message}";
            }
        }

        private void Close(Application app)
        {
            try
            {
                app.Quit();
            }
            catch (Exception ex)
            {
                _logger.ErrorFormat(ex, "Unable to close Powerpoint application {0}", ex.Message);
                //TODO: Try to kill the process.
            }
        }

        private void Close(Presentation doc)
        {
            try
            {
                doc.Close();
            }
            catch (Exception ex)
            {
                _logger.ErrorFormat(ex, "Unable to close presentation {0}", ex.Message);
            }
        }
    }
}
