using Castle.Core.Logging;
using Jarvis.DocumentStore.Shared.Jobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.Tika.Tests
{
    public abstract class BaseConversionTests : IPollerTest
    {
        public string Name
        {
            get
            {
                return GetName();
            }
        }

        protected abstract string GetName();

        public List<PollerTestResult> Execute()
        {
            List<PollerTestResult> retValue = new List<PollerTestResult>();

            ITikaAnalyzer analyzer = BuildAnalyzer();

            TestFile(retValue, analyzer, "test_doc.docx", "docx", "Lorem Ipsum", TestFiles.docx);
            TestFile(retValue, analyzer, "test_ppt.pptx", "pptx", "PPT TITLE", TestFiles.pptx);

            return retValue;
        }

        private static void TestFile(
            List<PollerTestResult> retValue, 
            ITikaAnalyzer analyzer,
            String fileName,
            String type,
            String expected,
            Byte[] fileContent)
        {
            var tempFile = Path.Combine(Path.GetTempPath(), fileName);
            if (File.Exists(tempFile)) File.Delete(tempFile);
            File.WriteAllBytes(tempFile, fileContent);
            try
            {
                string content = analyzer.GetHtmlContent(tempFile, "");
                if (content.Contains(expected))
                {
                    retValue.Add(new PollerTestResult(true, type + " conversion"));
                }
                else
                {
                    retValue.Add(new PollerTestResult(false, type + " conversion: wrong content"));
                }
            }
            catch (Exception ex)
            {
                retValue.Add(new PollerTestResult(false, type + " conversion: " + ex.Message));
            }
        }

        protected abstract ITikaAnalyzer BuildAnalyzer();
    }

    public class InProcessConversionTests : BaseConversionTests
    {
        protected override ITikaAnalyzer BuildAnalyzer()
        {
            return new TikaNetAnalyzer()
            {
                Logger = NullLogger.Instance
            };
        }

        protected override string GetName()
        {  
                return "In Process Tests";
        }
    }

    public class OutOfProcessConversionTests : BaseConversionTests
    {
        protected override ITikaAnalyzer BuildAnalyzer()
        {
            return new TikaNetAnalyzer()
            {
                Logger = NullLogger.Instance
            };
        }

        protected override string GetName()
        {
            return "Out Of Process Tests";
        }
    }
}
