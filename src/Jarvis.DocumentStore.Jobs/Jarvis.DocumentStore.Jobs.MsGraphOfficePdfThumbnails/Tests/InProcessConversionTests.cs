using Jarvis.DocumentStore.Shared.Jobs;
using System;
using System.Collections.Generic;
using System.IO;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;

namespace Jarvis.DocumentStore.Jobs.MsGraphOfficePdfThumbnails.Tests
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

        public MsGraphOfficePdfThumbnail MsGraphOfficePdfThumbnail { get; set; }

        public List<PollerTestResult> Execute()
        {
            List<PollerTestResult> retValue = new List<PollerTestResult>();

            TestFile(retValue, "document.pdf", TestFiles.Document);

            return retValue;
        }

        private void TestFile(List<PollerTestResult> retValue, string fileName, byte[] fileData)
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(path);
            try
            {
                var fn = Path.Combine(path, fileName);
                File.WriteAllBytes(fn, fileData);
                var conversion = MsGraphOfficePdfThumbnail.CreateThumbnail(fn, path);
                if (!string.IsNullOrEmpty(conversion.Result))
                {
                    retValue.Add(new PollerTestResult(true, $"Conversion of file {fileName} suceeded"));
                }
                else
                {
                    retValue.Add(new PollerTestResult(false, $"Conversion of file {fileName} failed"));
                }
            }
            catch (Exception ex)
            {
                retValue.Add(new PollerTestResult(false, $"Conversion of file {fileName} failed {ex.Message}"));
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }
    }
}
