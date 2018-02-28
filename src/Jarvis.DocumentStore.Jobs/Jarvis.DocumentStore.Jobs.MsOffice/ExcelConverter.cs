using Castle.Core.Logging;
using System;
using Microsoft.Office.Interop.Excel;

namespace Jarvis.DocumentStore.Jobs.MsOffice
{
#pragma warning disable S2583 // Conditionally executed blocks should be reachable
#pragma warning disable S1854 // Dead stores should be removed
    public class ExcelConverter
    {
        private readonly ILogger _logger;

        public ExcelConverter(ILogger logger)
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
            Workbook wkb = null;
            OfficeUtils.KillOfficeProcess("EXCEL");
            try
            {
                app = new Application();
                wkb = app.Workbooks.Open(sourcePath);
                wkb.ExportAsFixedFormat(XlFixedFormatType.xlTypePDF, targetPath);
                wkb.Close(false);
                wkb = null;
                app.Quit();
                app = null;
                return String.Empty;
            }
            catch (Exception ex)
            {
                _logger.ErrorFormat(ex, "Error converting {0} - {1}", sourcePath, ex.Message);

                if (wkb != null)
                {
                    this.Close(wkb);
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
                _logger.ErrorFormat(ex, "Unable to close excel application {0}", ex.Message);
                //TODO: Try to kill the process.
            }
        }

        private void Close(Workbook doc)
        {
            try
            {
                doc.Close(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorFormat(ex, "Unable to close Excel document {0}", ex.Message);
            }
        }
    }
#pragma warning restore S2583 // Conditionally executed blocks should be reachable
#pragma warning restore S1854 // Dead stores should be removed
}
