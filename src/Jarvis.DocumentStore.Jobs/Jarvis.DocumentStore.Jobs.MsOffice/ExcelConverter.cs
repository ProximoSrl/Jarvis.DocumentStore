using Castle.Core.Logging;
using System;
using Microsoft.Office.Interop.Excel;
using Jarvis.DocumentStore.JobsHost.Support;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Jarvis.DocumentStore.Jobs.MsOffice
{
#pragma warning disable S2583 // Conditionally executed blocks should be reachable
#pragma warning disable S1854 // Dead stores should be removed
    public class ExcelConverter : BaseConverter
    {
        private readonly IClientPasswordSet _clientPasswordSet;

        public ExcelConverter(IClientPasswordSet clientPasswordSet)
        {
            _clientPasswordSet = clientPasswordSet;
        }

        public string GetPassword(String fileName)
        {
            return _clientPasswordSet.GetPasswordFor(fileName).FirstOrDefault() ?? "fake password, to avoid being stuck with ask password dialog";
        }

        protected override String OnRunJob(JobData job)
        {
            Application app = null;
            Workbook wkb = null;
            var sourceFile = job.SourceFile;
            var destinationFile = job.DestinationFile;

            try
            {
                app = new Application();
                app.ScreenUpdating = false;
                app.DisplayStatusBar = false;
                app.EnableEvents = false;
                app.DisplayAlerts = false;
                app.DisplayClipboardWindow = false;

                //app.Visible = true;
                Logger.DebugFormat("Opening {0} in office", sourceFile);
                wkb = app.Workbooks.Open(sourceFile, Password: GetPassword(Path.GetFileName(sourceFile)));

                Logger.DebugFormat("Exporting {0} in pdf", sourceFile);
                HashSet<String> workbookWithPrintableAreaSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (Name name in wkb.Names)
                {
                    if (name.Name.IndexOf("print_area", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var splitted = name.Name.Split('!');
                        var workbookName = splitted[0].Trim('\"', '\'', ' ');
                        workbookWithPrintableAreaSet.Add(workbookName);
                    }
                }

                Boolean printAreaIsValid = false;
                Worksheet worksheetToPrint = (wkb.ActiveSheet as Worksheet);
                var activeSheetName = worksheetToPrint?.Name as String;
                if (!String.IsNullOrEmpty(activeSheetName) && workbookWithPrintableAreaSet.Contains(activeSheetName))
                {
                    printAreaIsValid = true; //current selected worksheet has a print area set
                }
                else
                {
                    //ok we need to find if any of the worksheet has a valid print area
                    foreach (var workbookName in workbookWithPrintableAreaSet)
                    {
                        var worksheet = wkb.Sheets
                            .OfType<Worksheet>()
                            .FirstOrDefault(s => s.Name?.Equals(workbookName, StringComparison.OrdinalIgnoreCase) == true &&
                                s.Visible == XlSheetVisibility.xlSheetVisible);
                        if (worksheet != null)
                        {
                            worksheetToPrint = worksheet;
                            printAreaIsValid = true; //current selected worksheet has a print area set
                            break;
                        }
                    }
                }

                //ok check if the current workbook has a print area defined

                if (printAreaIsValid)
                {
                    //we have a printable area, we already selected the active sheet, we 
                    //can now print everything, but pay attention, we want to print only 
                    //one worksheet, not all of them.
                    worksheetToPrint.ExportAsFixedFormat(
                        XlFixedFormatType.xlTypePDF,
                        destinationFile,
                        XlFixedFormatQuality.xlQualityStandard, //object quality
                        true, //include doc property
                        false, //ignore print areas
                        Type.Missing, //from
                        Type.Missing, //to
                        false,//false //open after publish
                        Type.Missing //null // fixedFormatExternalClass ....
                    );
                }
                else
                {
                    //There is no printable area, to avoid chaos we will print
                    //only the first page of the active sheet.
                    worksheetToPrint.ExportAsFixedFormat(
                        XlFixedFormatType.xlTypePDF,
                        destinationFile,
                        XlFixedFormatQuality.xlQualityStandard, //object quality
                        true, //include doc property
                        false, //ignore print areas
                        1, //from
                        1, //to
                        false //open after publish
                              //null // fixedFormatExternalClass ....
                   );
                }

                Logger.DebugFormat("Closing excel", sourceFile);
                Close(wkb);
                wkb = null;
                Logger.DebugFormat("Quitting excel", sourceFile);
                Close(app);
                app = null;
                return String.Empty;
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat(ex, "Error converting {0} - {1}", sourceFile, ex.Message);

                if (wkb != null)
                {
                    Close(wkb);
                }
                if (app != null)
                {
                    this.Close(app);
                }
                return $"Error converting {sourceFile} - {ex.Message}";
            }
        }

        /// <summary>
        /// Convert word file to pdf
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="targetPath"></param>
        /// <returns>Error message.</returns>
        internal String ConvertToPdf(string sourcePath, string targetPath)
        {
            var task = base.QueueJob(sourcePath, targetPath);
            //this will wait the task.
            return task.Result;
        }

        private void Close(Application app)
        {
            try
            {
                app.SafeClose();
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat(ex, "Unable to close excel application {0}", ex.Message);
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
                Logger.ErrorFormat(ex, "Unable to close Excel document {0}", ex.Message);
            }
        }
    }
#pragma warning restore S2583 // Conditionally executed blocks should be reachable
#pragma warning restore S1854 // Dead stores should be removed
}
