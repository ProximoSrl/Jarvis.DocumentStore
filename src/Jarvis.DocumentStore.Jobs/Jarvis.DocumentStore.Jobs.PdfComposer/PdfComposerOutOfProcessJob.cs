using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.JobsHost.Helpers;
using Jarvis.DocumentStore.Shared.Jobs;
using PdfSharp.Drawing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;

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
        private readonly HashSet<String> officeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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
                    pdfExists = await InnerGetPdf(workingFolder, client, files, handle, documentHandle, pdfExists).ConfigureAwait(false);
                }
                catch (System.Net.WebException ex)
                {
                    Logger.WarnFormat("Handle {0} has no PDF format", handle);
                }

                if (!pdfExists)
                {
                    int requeueCount = GetRequeueCount(parameters);
                    if (requeueCount <= 3) //first 3 times, always retry (lets DS the time to generate jobs)
                    {
                        return GenerateRequeueProcessResult(requeueCount);
                    }

                    //need to check if this file has some job pending that can generate pdf.
                    var pendingJobs = await client.GetJobsAsync(documentHandle);
                    var fileName = await GetfileNameFromHandle(client, documentHandle);
                    Boolean needWaitForJobToRun = CheckIfSomeJobCanStillProducePdfFormat(pendingJobs, fileName, requeueCount);

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

            await client.UploadAsync(finalFileName, new DocumentHandle(destinationHandle)).ConfigureAwait(false);
            return ProcessResult.Ok;
        }

        /// <summary>
        /// GEnerate a result that will re-queue the job using always greater wait time to wait for
        /// really long time (queue stuck) but avoid saturating job.
        /// </summary>
        /// <param name="requeueCount"></param>
        /// <returns></returns>
        private static ProcessResult GenerateRequeueProcessResult(Int32 requeueCount)
        {
            //The goal here is to have short wait for the first 10 iteration, then 
            //start waiting for long time, because if the result is not ready with few
            //iteration, probably the queue is stuck and we should wait for much longer.
            TimeSpan waitTime;
            if (requeueCount > 50)
            {
                return ProcessResult.Fail("Reached maximum number of retries");
            }
            else if (requeueCount > 20)
            {
                waitTime = TimeSpan.FromHours(1);
            }
            else if (requeueCount > 10)
            {
                waitTime = TimeSpan.FromMinutes(15);
            }
            else
            {
                waitTime = TimeSpan.FromSeconds(requeueCount * 10);
            }

            //some job is still executing, probably pdf format could be generated in the future
            return new ProcessResult(
                waitTime,
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

        private bool CheckIfSomeJobCanStillProducePdfFormat(Shared.Jobs.QueuedJobInfo[] pendingJobs, String fileName, Int32 retryCount)
        {
            //We wait for the job to be generated only for the first 10 wait.
            Boolean waitForNotGeneratedJob = retryCount < 10;

            //We have some job pending for the pdfConverter queue?
            if (CheckIfQueueJobShouldStillBeExecuted(pendingJobs, "pdfConverter", allowNullJob: waitForNotGeneratedJob))
            {
                //PdfConverter did not run, need to wait
                return true;
            }

            var extension = Path.GetExtension(fileName);
            if (!String.IsNullOrEmpty(extension) && officeExtensions.Contains(extension.Trim('.')))
            {
                //this is a file that can be converted with office, so we should check office queue, maybe pdf is still pending
                if (CheckIfQueueJobShouldStillBeExecuted(pendingJobs, "office", allowNullJob: waitForNotGeneratedJob))
                {
                    //The extension is an extension of office, but office queue still did not run.
                    return true;
                }
            }

            //ok we can wait if we have NO job, maybe the queue is blocked and we do not have any job still scheduled.
            return pendingJobs.Count() == 0;
        }

        /// <summary>
        /// Check if a job is pending for a specific queue given the list of queue job info for the handle.
        /// </summary>
        /// <param name="pendingJobs"></param>
        /// <param name="queueName"></param>
        /// <param name="allowNullJob">
        /// If this parameter is true, a null job for the queue will be considered still a job that needs to be executed because
        /// job was still not generated by the service.
        /// 
        /// We have a situation, when we check
        /// for a specific job queue, as an example, office queue, the job is not executed if the job is null or is pending
        /// or re-queued. If we have a bug in the server queues that NEVER generates the job, the job will be null forever, this means
        /// that we can accept that job is null only for the first X request.</param>
        /// <returns></returns>
        private bool CheckIfQueueJobShouldStillBeExecuted(QueuedJobInfo[] pendingJobs, String queueName, Boolean allowNullJob)
        {
            var job = pendingJobs.FirstOrDefault(j => j.QueueName == queueName);

            if (!allowNullJob && job == null)
            {
                Logger.Warn($"Job for queue {queueName} was not generated");
                return false;
            }

            //we simply return true if the job is not executed, it is still pending or re-queued.
            if (job?.Executed != true)
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
