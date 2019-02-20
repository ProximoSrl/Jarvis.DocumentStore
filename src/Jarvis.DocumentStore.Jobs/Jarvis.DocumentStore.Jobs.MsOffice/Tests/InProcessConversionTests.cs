using Jarvis.DocumentStore.Shared.Jobs;
using System;
using System.Collections.Generic;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;

namespace Jarvis.DocumentStore.Jobs.MsOffice.Tests
{
    public class BaseConversionTests : IPollerTest
    {
        public string Name
        {
            get
            {
                return "Document conversions";
            }
        }

        public WordConverter WordConverter { get; set; }

        public PowerPointConverter PowerPointConverter { get; set; }

        public ExcelConverter ExcelConverter { get; set; }

        public List<PollerTestResult> Execute()
        {
            List<PollerTestResult> retValue = new List<PollerTestResult>();

            TestWordFile(retValue, "office_test_doc.docx", "docx", TestFiles.docx);
            TestPowerPointFile(retValue, "office_test_ppt.pptx", "pptx", TestFiles.pptx);
            TestExcelFile(retValue, "office_test_excel.xlsx", "xlsx", TestFiles.Excel);

            return retValue;
        }

        private void TestWordFile(
            List<PollerTestResult> retValue,
            String fileName,
            String type,
            Byte[] fileContent)
        {
            try
            {
                var tempFile = Path.Combine(Path.GetTempPath(), fileName);
                if (File.Exists(tempFile)) File.Delete(tempFile);
                File.WriteAllBytes(tempFile, fileContent);

                var conversionError = WordConverter.ConvertToPdf(tempFile, tempFile+ ".pdf");
                if (!String.IsNullOrEmpty(conversionError))
                {
                    retValue.Add(new PollerTestResult(false, type + "Conversion with word converter failed: " + conversionError));
                }
                else
                {
                    retValue.Add(new PollerTestResult(true, type + "Conversion with word ok."));
                }
            }
            catch (Exception ex)
            {
                retValue.Add(new PollerTestResult(false, type + "Conversion with word converter failed: " + ex.Message));
            }
        }

        private void TestPowerPointFile(
          List<PollerTestResult> retValue,
          String fileName,
          String type,
          Byte[] fileContent)
        {
            try
            {
                var tempFile = Path.Combine(Path.GetTempPath(), fileName);
                if (File.Exists(tempFile)) File.Delete(tempFile);
                File.WriteAllBytes(tempFile, fileContent);

                var conversionError = PowerPointConverter.ConvertToPdf(tempFile, tempFile + ".pdf");
                if (!String.IsNullOrEmpty( conversionError))
                {
                    retValue.Add(new PollerTestResult(false, type + "Conversion with powerpoint converter failed:" + conversionError));
                }
                else
                {
                    retValue.Add(new PollerTestResult(true, type + "Conversion with powerpoint ok."));
                }
            }
            catch (Exception ex)
            {
                retValue.Add(new PollerTestResult(false, type + "Conversion with powerpoint converter failed: " + ex.Message));
            }
        }

        private void TestExcelFile(
            List<PollerTestResult> retValue,
            String fileName,
            String type,
            Byte[] fileContent)
        {
            try
            {
                var tempFile = Path.Combine(Path.GetTempPath(), fileName);
                if (File.Exists(tempFile)) File.Delete(tempFile);
                File.WriteAllBytes(tempFile, fileContent);

                var conversionError = ExcelConverter.ConvertToPdf(tempFile, tempFile + ".pdf");
                if (!String.IsNullOrEmpty(conversionError))
                {
                    retValue.Add(new PollerTestResult(false, type + "Conversion with excel converter failed:" + conversionError));
                }
                else
                {
                    retValue.Add(new PollerTestResult(true, type + "Conversion with excel ok."));
                }
            }
            catch (Exception ex)
            {
                retValue.Add(new PollerTestResult(false, type + "Conversion with word converter failed: " + ex.Message));
            }
        }
    }
}
