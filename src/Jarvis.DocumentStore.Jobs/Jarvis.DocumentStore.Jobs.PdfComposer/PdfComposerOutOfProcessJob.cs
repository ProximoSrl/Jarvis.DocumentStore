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

namespace Jarvis.DocumentStore.Jobs.PdfComposer
{
    public class PdfComposerOutOfProcessJob : AbstractOutOfProcessPollerJob
    {
        public PdfComposerOutOfProcessJob()
        {
            base.PipelineId = "pdfComposer";
            base.QueueName = "pdfComposer";
        }

        protected async override Task<ProcessResult> OnPolling(
            Shared.Jobs.PollerJobParameters parameters,
            string workingFolder)
        {

            var client = GetDocumentStoreClient(parameters.TenantId);
            var handles = parameters.All["documentList"].Split('|');
            var destinationHandle = parameters.All["resultingDocumentHandle"];
            List<FileToComposeData> files = new List<FileToComposeData>();
            foreach (var handle in handles)
            {
                var documentHandle = new DocumentHandle(handle);
                Boolean pdfExists = false;
                try
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
                }
                catch (System.Net.WebException ex)
                {
                    Logger.WarnFormat("Handle {0} has no PDF format", handle);
                }

                if (!pdfExists)
                {
                    //need to check if this file has some job pending.
                    var pendingJobs = await client.GetPendingJobsAsync(documentHandle);
                    if (pendingJobs.Length > 0)
                    {
                        //some job is still executing, probably pdf format could be generated in the future
                        return new ProcessResult(TimeSpan.FromSeconds(10));
                    }
                    else
                    {
                        //This file has no pdf format, mark as missing pdf.
                        var fileName = await GetfileNameFromHandle(client, documentHandle);
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

            String finalFileName = Path.Combine(workingFolder, Guid.NewGuid() + ".pdf");
            manipulator.Save(finalFileName);

            var result = await client.UploadAsync(finalFileName, new DocumentHandle(destinationHandle));
            return ProcessResult.Ok;
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
                    gfx.DrawString("Handle " + documentHandle + " fileName " + fileName +  " has no pdf format",
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
