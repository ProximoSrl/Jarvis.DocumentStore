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
    public class WordConverter : BaseConverter
    {
        private readonly IClientPasswordSet _clientPasswordSet;

        public WordConverter(IClientPasswordSet clientPasswordSet)
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
            var sourceFile = job.SourceFile;
            var destinationFile = job.DestinationFile;
            Document doc = null;
            try
            {
                app = new Application();
                app.DisplayAlerts = WdAlertLevel.wdAlertsNone;

                Logger.DebugFormat("Opening {0} in office", sourceFile);
                //it is importantì
                doc = app.Documents.Open(sourceFile, PasswordDocument: GetPassword(Path.GetFileName(sourceFile)));
                Logger.InfoFormat("About to converting file {0} to pfd", sourceFile);
                doc.SaveAs2(destinationFile, WdSaveFormat.wdFormatPDF);
                Logger.DebugFormat("File {0} converted to pdf. Closing word", sourceFile);
                doc.Close();
                doc = null;
                Logger.DebugFormat("Application quit.", sourceFile);
                app.Quit();
                app = null;
                return String.Empty;
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat(ex, "Error converting {0} - {1}", sourceFile, ex.Message);

                if (doc != null)
                {
                    this.Close(doc);
                }
                if (app != null)
                {
                    this.Close(app);
                }
                return $"Error converting {sourceFile} - {ex.Message}";
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
                Logger.ErrorFormat(ex, "Unable to close word application {0}", ex.Message);
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
                Logger.ErrorFormat(ex, "Unable to close document {0}", ex.Message);
            }
        }

        internal String ConvertToPdf(string sourcePath, string targetPath)
        {
            var task = base.QueueJob(sourcePath, targetPath);
            //this will wait the task.
            return task.Result;
        }
    }
#pragma warning restore S2583 // Conditionally executed blocks should be reachable
#pragma warning restore S1854 // Dead stores should be removed
}
