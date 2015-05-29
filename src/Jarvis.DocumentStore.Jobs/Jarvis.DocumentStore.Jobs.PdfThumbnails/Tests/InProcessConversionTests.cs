using Castle.Core.Logging;
using Jarvis.DocumentStore.Jobs.PdfThumbnails;
using Jarvis.DocumentStore.Jobs.PdfThumbnails.Tests;
using Jarvis.DocumentStore.Shared.Jobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.Tika.Tests
{
    public class BaseConversionTests : IPollerTest
    {
        public string Name
        {
            get
            {
                return "Verify pdf to image converter";
            }
        }

        public Func<CreateImageFromPdfTask> Factory { get; set; }

        public List<PollerTestResult> Execute()
        {
            List<PollerTestResult> retValue = new List<PollerTestResult>();

            var task = Factory();

            TestFile(retValue, task, "test.pdf", TestFiles.SimplePdf);

            return retValue;
        }

        private static void TestFile(
            List<PollerTestResult> retValue,
            CreateImageFromPdfTask task,
            String fileName,
            Byte[] fileContent)
        {
            var tempFile = Path.Combine(Path.GetTempPath(), fileName);
            if (File.Exists(tempFile)) File.Delete(tempFile);
            File.WriteAllBytes(tempFile, fileContent);
            try
            {
                var convertParams = new CreatePdfImageTaskParams()
                {
                    Dpi = 150,
                    FromPage = 1,
                    Pages = 1,
                    Format = CreatePdfImageTaskParams.ImageFormat.Jpg,
                };
                Boolean wasCalled = false;
                var result = task.Run(
                    tempFile,
                    convertParams,
                    (i, s) =>
                    {
                        wasCalled = true;
                        return Task.FromResult<Boolean>(true);
                    }
                );
                result.Wait();
                if (wasCalled)
                {
                    retValue.Add(new PollerTestResult(true, "Pdf to Jpg"));
                }
                else
                {
                    retValue.Add(new PollerTestResult(false, "Pdf to Jpg"));
                }
            }
            catch (Exception ex)
            {
                retValue.Add(new PollerTestResult(false, "Pdf to Jpg: " + ex.Message));
                throw;
            }
        }

    }

   
}
