using Castle.Core.Logging;
using Jarvis.DocumentStore.Jobs.LibreOffice;
using Jarvis.DocumentStore.Jobs.LibreOffice.Tests;
using Jarvis.DocumentStore.Shared.Jobs;
using System;
using System.Collections.Generic;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
namespace Jarvis.DocumentStore.Jobs.Tika.Tests
{
    public class BaseConversionTests : IPollerTest
    {
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public string Name
        {
            get
            {
                return "Document conversions";
            }
        }

        public ILibreOfficeConversion Conversion { get; set; }

        public List<PollerTestResult> Execute()
        {
            List<PollerTestResult> retValue = new List<PollerTestResult>();

            TestFile(retValue, "office_test_doc.docx", "docx", TestFiles.docx);
            TestFile(retValue, "office_test_ppt.pptx", "pptx", TestFiles.pptx);

            return retValue;
        }

        private void TestFile(
            List<PollerTestResult> retValue,
            String fileName,
            String type,
            Byte[] fileContent)
        {
            String converter = Conversion.GetType().Name;
            try
            {
                var tempFile = Path.Combine(Path.GetTempPath(), fileName);
                if (File.Exists(tempFile)) File.Delete(tempFile);
                File.WriteAllBytes(tempFile, fileContent);

                string content = Conversion.Run(tempFile, "pdf");
                if (!String.IsNullOrEmpty(content))
                {
                    retValue.Add(new PollerTestResult(true, type + " conversion with converter: " + converter));
                }
                else
                {
                    retValue.Add(new PollerTestResult(false, type + " conversion: wrong content with converter: " + converter));
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat(ex, "Unable to execute initial test {0}", ex.Message);
                retValue.Add(new PollerTestResult(false, type + " conversion with converter " + converter + ex.Message));
            }
        }
    }
}
