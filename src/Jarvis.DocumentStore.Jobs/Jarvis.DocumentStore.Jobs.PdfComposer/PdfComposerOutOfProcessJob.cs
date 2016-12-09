using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.JobsHost.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using PdfSharp.Drawing;
using Jarvis.DocumentStore.Shared.Jobs;

namespace Jarvis.DocumentStore.Jobs.PdfComposer
{
    public class PdfComposerOutOfProcessJob : AbstractOutOfProcessPollerJob
    {
        private const String ReQueueCountParameterName = "requeue_count";

        public PdfComposerOutOfProcessJob()
        {
            base.PipelineId = "pdfComposer";
            base.QueueName = "pdfComposer";
        }

        /// <summary>
        /// These are the extension that trigger office queue, remember that office queue
        /// is the queue that is capable of converting an office document into pdf.
        /// </summary>
        private HashSet<String> officeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "xls", "xlsx", "docx", "doc", "ppt", "pptx", "pps", "ppsx", "rtf", "odt", "ods", "odp"
        };

        protected async override Task<ProcessResult> OnPolling(
            Shared.Jobs.PollerJobParameters parameters,
            string workingFolder)
        {
            var client = GetDocumentStoreClient(parameters.TenantId);
            var handles = parameters.All["documentList"].Split('|');
            var destinationHandle = parameters.All["resultingDocumentHandle"];
            var destinationFileName = parameters.All["resultingDocumentFileName"];
            if (!destinationFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                destinationFileName = Path.ChangeExtension(destinationFileName, ".pdf");
            }
            List<FileToComposeData> files = new List<FileToComposeData>();
            foreach (var handle in handles)
            {
                var documentHandle = new DocumentHandle(handle);
                Boolean pdfExists = false;
                try
                {
                    pdfExists = await InnerGetPdf(workingFolder, client, files, handle, documentHandle, pdfExists);
                }
                catch (System.Net.WebException ex)
                {
                    Logger.WarnFormat("Handle {0} has no PDF format", handle);
                }

                if (!pdfExists)
                {
                    int requeueCount = GetRequeueCount(parameters);
                    if (requeueCount <= 3) //first 3 times, always retry (lets DS the time to generate jobs)
                        return GenerateRequeueProcessResult(requeueCount);

                    //need to check if this file has some job pending that can generate pdf.
                    var pendingJobs = await client.GetJobsAsync(documentHandle);
                    var fileName = await GetfileNameFromHandle(client, documentHandle);
                    Boolean needWaitForJobToRun = CheckIfSomeJobCanStillProducePdfFormat(pendingJobs, fileName);

                    //need to check if queue that can convert the document are still running. We need to wait for the queue to be stable.
                    if (needWaitForJobToRun)
                    {
                        return GenerateRequeueProcessResult(requeueCount);
                    }
                    else
                    {
                        //This file has no pdf format, mark as missing pdf.
                        Logger.WarnFormat("Handle {0} has no pdf format, status of queue is {1}", handle, String.Join(",", pendingJobs.Select(j => String.Format("{0}[Executed:{1} Success:{2}]", j.QueueName, j.Executed, j.Success))));
                        files.Add(FileToComposeData.NoPdfFormat(handle, fileName));
                    }
                }
            }
            //now compose everything.
            PdfManipulator manipulator = new PdfManipulator(Logger); //Create a manipulator
            foreach (var fileToCompose in files)
            {
                String pdfFileToAppend = fileToCompose.PdfFileName;
                if (!fileToCompose.HasPdfFormat)
                {
                    pdfFileToAppend = GeneratePlaceholderFile(workingFolder, fileToCompose.FileName, fileToCompose.DocumentHandle);
                }

                var error = manipulator.AppendDocumentAtEnd(pdfFileToAppend);
                if (!String.IsNullOrEmpty(error))
                {
                    throw new ApplicationException(String.Format("Unable to compose file {0} error {1}", fileToCompose.DocumentHandle, error));
                }
            }

            manipulator.AddPageNumber();

            String outputDirectory = Path.Combine(workingFolder, Guid.NewGuid().ToString());
            Directory.CreateDirectory(outputDirectory);
            var finalFileName = Path.Combine(outputDirectory, destinationFileName);
            manipulator.Save(finalFileName);

            var result = await client.UploadAsync(finalFileName, new DocumentHandle(destinationHandle));
            return ProcessResult.Ok;
        }

        private static ProcessResult GenerateRequeueProcessResult(Int32 requeueCount)
        {
            //some job is still executing, probably pdf format could be generated in the future

            Int32 secondsToWait = Math.Min(requeueCount * 2, 30);
            return new ProcessResult(
                TimeSpan.FromSeconds(secondsToWait),
                new Dictionary<String, String>()
                {
                      {ReQueueCountParameterName, (requeueCount + 1).ToString() }
                });
        }

        private static int GetRequeueCount(Shared.Jobs.PollerJobParameters parameters)
        {
            Int32 requeueCount;
            String requeueCountParameterValue;
            if (parameters.All.TryGetValue(ReQueueCountParameterName, out requeueCountParameterValue))
            {
                requeueCount = Int32.Parse(requeueCountParameterValue);
            }
            else
            {
                requeueCount = 0;
            }

            return requeueCount;
        }

        private static async Task<bool> InnerGetPdf(string workingFolder, DocumentStoreServiceClient client, List<FileToComposeData> files, string handle, DocumentHandle documentHandle, bool pdfExists)
        {
            var pdfData = client.OpenRead(documentHandle, DocumentFormats.Pdf);
            var tempFile = Path.Combine(workingFolder, Guid.NewGuid() + ".pdf");
            using (var downloaded = new FileStream(tempFile, FileMode.OpenOrCreate, FileAccess.Write))
            {
                var stream = await pdfData.OpenStream();
                stream.CopyTo(downloaded);
            }
            files.Add(FileToComposeData.FromDownloadedPdf(tempFile, handle));
            pdfExists = true;
            return pdfExists;
        }

        private bool CheckIfSomeJobCanStillProducePdfFormat(Shared.Jobs.QueuedJobInfo[] pendingJobs, String fileName)
        {
            if (CheckIfQueueJobShouldStillBeExecuted(pendingJobs, "pdfConverter"))
            {
                //PdfConverter did not run, need to wait
                return true;
            }

            var extension = Path.GetExtension(fileName);
            if (!String.IsNullOrEmpty(extension) && officeExtensions.Contains(extension.Trim('.')))
            {
                //this is a file that can be converted with office.
                if (CheckIfQueueJobShouldStillBeExecuted(pendingJobs, "office"))
                {
                    //The extension is an extension of office, but office queue still did not run.
                    return true;
                }
            }

            var jobThatStillWereNotRun = pendingJobs.Count(j => j.Executed == false);
            //we do not know if there are some more jobs that can produce pdf, we assume that no pdf format is present
            //if this handle has no more job to run.
            return jobThatStillWereNotRun == 0;
        }

        private bool CheckIfQueueJobShouldStillBeExecuted(Shared.Jobs.QueuedJobInfo[] pendingJobs, String queueName)
        {
            var job = pendingJobs.FirstOrDefault(j => j.QueueName == queueName);
            if (job == null || job.Executed == false)
            {
                return true;
            }

            return false;
        }

        private static string GeneratePlaceholderFile(string workingFolder, string fileName, string documentHandle)
        {
            string pdfFileToAppend = Path.Combine(workingFolder, Guid.NewGuid() + ".pdf");
            using (PdfSharp.Pdf.PdfDocument document = new PdfSharp.Pdf.PdfDocument())
            {
                // Create an empty page
                PdfSharp.Pdf.PdfPage page = document.AddPage();

                // Get an XGraphics object for drawing
                using (XGraphics gfx = XGraphics.FromPdfPage(page))
                {
                    // Create a font
                    XFont font = new XFont("Verdana", 14, XFontStyle.Bold);

                    // Draw the text
                    gfx.DrawString("Handle " + documentHandle + " fileName " + fileName + " has no pdf format",
                        font,
                        XBrushes.Black,
                      new XRect(0, 0, page.Width, page.Height),
                      XStringFormats.TopCenter);
                }

                document.Save(pdfFileToAppend);
            }

            return pdfFileToAppend;
        }

        private async Task<string> GetfileNameFromHandle(DocumentStoreServiceClient client, DocumentHandle handle)
        {
            try
            {
                return await client.GetFileNameAsync(handle);
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        private class FileToComposeData
        {
            public String PdfFileName { get; private set; }

            public String DocumentHandle { get; private set; }

            public Boolean HasPdfFormat { get { return !String.IsNullOrEmpty(PdfFileName); } }

            public String FileName { get; private set; }

            internal static FileToComposeData FromDownloadedPdf(string pdfFile, string documentHandle)
            {
                return new FileToComposeData()
                {
                    PdfFileName = pdfFile,
                    DocumentHandle = documentHandle,
                };
            }

            internal static FileToComposeData NoPdfFormat(string documentHandle, String fileName)
            {
                return new FileToComposeData()
                {
                    PdfFileName = null,
                    FileName = fileName,
                    DocumentHandle = documentHandle,
                };
            }
        }
    }
}
