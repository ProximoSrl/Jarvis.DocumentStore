using Castle.Core.Logging;
using Microsoft.Office.Interop.Word;
using System;

namespace Jarvis.DocumentStore.Jobs.MsOffice
{
#pragma warning disable S2583 // Conditionally executed blocks should be reachable
#pragma warning disable S1854 // Dead stores should be removed
    public class WordConverter
    {
        private readonly ILogger _logger;

        public WordConverter(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Convert word file to pdf
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="targetPath"></param>
        /// <returns>Error message.</returns>
        internal String ConvertToPdf(string sourcePath, string targetPath)
        {
            Application app = null;
            Document doc = null;
            OfficeUtils.KillOfficeProcess("WINWORD");
            try
            {
                app = new Application();
                doc = app.Documents.Open(sourcePath);
                doc.SaveAs2(targetPath, WdSaveFormat.wdFormatPDF);
                _logger.InfoFormat("File {0} converted to pdf.", sourcePath);
                doc.Close();
                doc = null;
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
