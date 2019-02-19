using Castle.Core.Logging;
using Jarvis.DocumentStore.JobsHost.Support;
using Microsoft.Office.Interop.Word;
using System;
using System.IO;
using System.Linq;

namespace Jarvis.DocumentStore.Jobs.MsOffice
{
#pragma warning disable S2583 // Conditionally executed blocks should be reachable
#pragma warning disable S1854 // Dead stores should be removed
    public class WordConverter
    {
        private readonly ILogger _logger;
        private readonly IClientPasswordSet _clientPasswordSet;

        public WordConverter(ILogger logger, IClientPasswordSet clientPasswordSet)
        {
            _logger = logger;
            _clientPasswordSet = clientPasswordSet;
        }

        public string GetPassword(String fileName)
        {
            return _clientPasswordSet.GetPasswordFor(fileName).FirstOrDefault() ?? "fake password, to avoid being stuck with ask password dialog";
        }
    
        /// <summary>
        /// Convert word file to pdf
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="targetPath"></param>
        /// <returns>Error message.</returns>
        internal String ConvertToPdf(string sourcePath, string targetPath, Boolean killEveryOtherWordProcess)
        {
            Application app = null;

            Document doc = null;
            if (killEveryOtherWordProcess)
            {
                OfficeUtils.KillOfficeProcess("WINWORD");
            }
            try
            {
                app = new Application();
                app.DisplayAlerts = WdAlertLevel.wdAlertsNone;

                _logger.DebugFormat("Opening {0} in office", sourcePath);
                //it is importantì
                doc = app.Documents.Open(sourcePath, PasswordDocument: GetPassword(Path.GetFileName(sourcePath)));
                _logger.InfoFormat("About to converting file {0} to pfd", sourcePath);
                doc.SaveAs2(targetPath, WdSaveFormat.wdFormatPDF);
                _logger.DebugFormat("File {0} converted to pdf. Closing word", sourcePath);
                doc.Close();
                doc = null;
                _logger.DebugFormat("Application quit.", sourcePath);
                app.Quit();
                app = null;
                return String.Empty;
            }
            catch (Exception ex)
            {
                _logger.ErrorFormat(ex, "Error converting {0} - {1}", sourcePath, ex.Message);

                if (doc != null)
                {
                    this.Close(doc);
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
                _logger.ErrorFormat(ex, "Unable to close word application {0}", ex.Message);
                //TODO: Try to kill the process.
            }
        }

        private void Close(Document doc)
        {
            try
            {
                doc.Close();
            }
            catch (Exception ex)
            {
                _logger.ErrorFormat(ex, "Unable to close document {0}", ex.Message);
            }
        }
    }
#pragma warning restore S2583 // Conditionally executed blocks should be reachable
#pragma warning restore S1854 // Dead stores should be removed
}
