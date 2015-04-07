using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.DocumentStore.JobsHost.Support;
using Jarvis.DocumentStore.Tests.Support;
using NUnit.Framework;
using Jarvis.DocumentStore.Jobs.LibreOffice;

namespace Jarvis.DocumentStore.Tests.PipelineTests
{
    [TestFixture(Category = "integration")]
    public class ConvertToPdfTaskTests
    {
        LibreOfficeConversion _withLibreOfficeConversion;
        LibreOfficeUnoConversion _unoConversion;
        private readonly IDictionary<string, string> _mapping = new Dictionary<string, string>();

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            MongoDbTestConnectionProvider.DropTestsTenant();

            _mapping["docx"] = TestConfig.PathToWordDocument;
            _mapping["xlsx"] = TestConfig.PathToExcelDocument;
            _mapping["pptx"] = TestConfig.PathToPowerpointDocument;
            _mapping["ppsx"] = TestConfig.PathToPowerpointShow;
            _mapping["txt"] = TestConfig.PathToTextDocument;
            _mapping["odt"] = TestConfig.PathToOpenDocumentText;
            _mapping["ods"] = TestConfig.PathToOpenDocumentSpreadsheet;
            _mapping["odp"] = TestConfig.PathToOpenDocumentPresentation;
            _mapping["rtf"] = TestConfig.PathToRTFDocument;

            _withLibreOfficeConversion = new LibreOfficeConversion(new JobsHostConfiguration())
            {
                Logger = new ConsoleLogger()
            };

            _unoConversion = new LibreOfficeUnoConversion(new JobsHostConfiguration())
            {
                Logger = new ConsoleLogger()
            };

            _unoConversion.CloseOpenOffice();
        }

        [SetUp]
        public void SetUp()
        {
            try
            {
                _unoConversion.CloseOpenOffice();
            }
            catch 
            {
            }
        }

        [Test]
        [TestCase("docx")]
        [TestCase("xlsx")]
        [TestCase("pptx")]
        [TestCase("ppsx")]
        [TestCase("txt")]
        [TestCase("odt")]
        [TestCase("ods")]
        [TestCase("odp")]
        [TestCase("rtf")]
        public void processing_file_should_succeed(string blobId)
        {
            var s = new Stopwatch();
            s.Start();
            var fileName = _withLibreOfficeConversion.Run(_mapping[blobId], "pdf");
            s.Stop();
            File.Delete(fileName);
            Debug.WriteLine("{0} conversion took {1} ms", blobId, s.ElapsedMilliseconds);
        }

        [Test]
        [TestCase("docx")]
        [TestCase("xlsx")]
        [TestCase("pptx")]
        [TestCase("ppsx")]
        [TestCase("txt")]
        [TestCase("odt")]
        [TestCase("ods")]
        [TestCase("odp")]
        [TestCase("rtf")]
        public void processing_file_with_sdk_should_succeed(string blobId) 
        {
            var s = new Stopwatch();
            s.Start();
            var fileName = _unoConversion.Run(_mapping[blobId], "pdf");
            s.Stop();
            File.Delete(fileName);
            Debug.WriteLine("{0} conversion took {1} ms", blobId, s.ElapsedMilliseconds);
        }

        [Test]
        public void parallel_conversion_should_not_throw_exceptions()
        {
            Parallel.ForEach(
                _mapping.Keys,
//                new ParallelOptions() { MaxDegreeOfParallelism = 2 },
                k =>
                {
                    var fileName = _unoConversion.Run(_mapping[k], "pdf");
                    File.Delete(fileName);
                }
            );
        }
    }
}
