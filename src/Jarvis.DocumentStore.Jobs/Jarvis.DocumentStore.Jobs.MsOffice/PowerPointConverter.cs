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
    public class PowerPointConverter : BaseConverter
    {
        private readonly IClientPasswordSet _clientPasswordSet;

        public PowerPointConverter(IClientPasswordSet clientPasswordSet)
        {
            _clientPasswordSet = clientPasswordSet;
        }

        public string GetPassword(String fileName)
        {
            return _clientPasswordSet.GetPasswordFor(fileName).FirstOrDefault() ?? "fake password, to avoid being stuck with ask password dialog";
        }

        protected override string OnRunJob(JobData job)
        {
            Application app = null;

            Presentation presentation = null;
            var sourceFile = job.SourceFile;
            var destinationFile = job.DestinationFile;
            try
            {
                app = new Application();
                app.DisplayAlerts = PpAlertLevel.ppAlertsNone;
                Logger.InfoFormat("Opening {0} in Powerpoint", sourceFile);
                //app.Visible = MsoTriState.msoFalse;
                //app.WindowState = PpWindowState.ppWindowMinimized;
                presentation = app.Presentations.Open(
                    sourceFile,
                    MsoTriState.msoFalse,
                    MsoTriState.msoFalse,
                    MsoTriState.msoFalse);

                Logger.DebugFormat("Delegate conversion {0} in PowerPoint", sourceFile);
                presentation.ExportAsFixedFormat(
                    destinationFile,
                    PpFixedFormatType.ppFixedFormatTypePDF,
                    PpFixedFormatIntent.ppFixedFormatIntentScreen);

                presentation.Close();
                presentation = null;

                Close(app);
                app = null;

                return String.Empty;
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat(ex, "Error converting {0} - {1}", sourceFile, ex.Message);

                if (presentation != null)
                {
                    Close(presentation);
                }
                if (app != null)
                {
                    Close(app);
                }
                return $"Error converting {sourceFile} - {ex.Message}";
            }
        }

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
                Logger.ErrorFormat(ex, "Unable to kill Powerpoint application {0}", ex.Message);
                //I do not care if the application generate exception during kill.  
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
                Logger.ErrorFormat(ex, "Unable to close presentation {0}", ex.Message);
            }
        }
    }
}
