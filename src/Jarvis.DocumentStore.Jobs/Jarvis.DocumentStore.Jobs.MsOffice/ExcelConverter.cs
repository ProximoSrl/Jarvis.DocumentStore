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
    public class ExcelConverter
    {
        private readonly ILogger _logger;
        private readonly IClientPasswordSet _clientPasswordSet;

        public ExcelConverter(ILogger logger, IClientPasswordSet clientPasswordSet)
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
        internal String ConvertToPdf(string sourcePath, string targetPath, Boolean killEveryOtherExcelProcess)
        {
            Application app = null;
            Workbook wkb = null;
            if (killEveryOtherExcelProcess)
            {
                OfficeUtils.KillOfficeProcess("EXCEL");
            }

            try
            {
                app = new Application();
                app.ScreenUpdating = false;
                app.DisplayStatusBar = false;
                app.EnableEvents = false;
                app.DisplayAlerts = false;
                app.DisplayClipboardWindow = false;

                //app.Visible = true;
                _logger.DebugFormat("Opening {0} in office", sourcePath);
                wkb = app.Workbooks.Open(sourcePath, Password: GetPassword(Path.GetFileName(sourcePath)));
                
                _logger.DebugFormat("Exporting {0} in pdf", sourcePath);
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
                            .FirstOrDefault(s => s.Name?.Equals(workbookName, StringComparison.OrdinalIgnoreCase) == true);
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
                    //can now print everything.

                    //ActiveSheet.ExportAsFixedFormat Type:= xlTypePDF, Filename:= _
                    //    "C:\temp\jarvis\docs\MicrosoftOfficePdfOutOfProcessJob\2e99ddb9-20eb-4566-87d8-245462dc442f\excel.pdf" _
                    //    , Quality:= xlQualityStandard, IncludeDocProperties:= True, IgnorePrintAreas _
                    //    := False, OpenAfterPublish:= False

                    //(wkb.ActiveSheet as Worksheet).PageSetup.PrintArea = "Print_Area";

                    //wkb.ExportAsFixedFormat(
                    //    Type: XlFixedFormatType.xlTypePDF,
                    //    Filename: targetPath,
                    //    Quality: XlFixedFormatQuality.xlQualityStandard,
                    //    IgnorePrintAreas: false,
                    //    OpenAfterPublish: false);

                    worksheetToPrint.ExportAsFixedFormat(
                        XlFixedFormatType.xlTypePDF,
                        targetPath,
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
                        targetPath,
                        XlFixedFormatQuality.xlQualityStandard, //object quality
                        true, //include doc property
                        false, //ignore print areas
                        1, //from
                        1, //to
                        false //open after publish
                        //null // fixedFormatExternalClass ....
                   );
                }

                _logger.DebugFormat("closing excel", sourcePath);
                wkb.Close(false);
                wkb = null;
                _logger.DebugFormat("Quitting excel", sourcePath);
                app.Quit();
                app.Kill();
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
