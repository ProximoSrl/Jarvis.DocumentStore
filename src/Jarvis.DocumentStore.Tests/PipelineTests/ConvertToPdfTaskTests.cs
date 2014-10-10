using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Tests.Support;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.PipelineTests
{
    [TestFixture(Category = "integration")]
    public class ConvertToPdfTaskTests
    {
        LibreOfficeConversion _withLibreOfficeConversion;
        LibreOfficeUnoConversion _unoConversion;
        private IDictionary<string, string> _mapping = new Dictionary<string, string>();

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            MongoDbTestConnectionProvider.TestDb.Drop();

            _mapping["docx"] = TestConfig.PathToWordDocument;
            _mapping["xlsx"] = TestConfig.PathToExcelDocument;
            _mapping["pptx"] = TestConfig.PathToPowerpointDocument;
            _mapping["ppsx"] = TestConfig.PathToPowerpointShow;
            _mapping["txt"] = TestConfig.PathToTextDocument;
            _mapping["odt"] = TestConfig.PathToOpenDocumentText;
            _mapping["ods"] = TestConfig.PathToOpenDocumentSpreadsheet;
            _mapping["odp"] = TestConfig.PathToOpenDocumentPresentation;
            _mapping["rtf"] = TestConfig.PathToRTFDocument;

            _withLibreOfficeConversion = new LibreOfficeConversion(new ConfigService())
            {
                Logger = new ConsoleLogger()
            };

            _unoConversion = new LibreOfficeUnoConversion(new ConfigService())
            {
                Logger = new ConsoleLogger()
            };
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
        public void processing_file_should_succeed(string fileId)
        {
            var s = new Stopwatch();
            s.Start();
            _withLibreOfficeConversion.Run(_mapping[fileId], "pdf");
            s.Stop();
            Debug.WriteLine("{0} conversion took {1} ms", fileId, s.ElapsedMilliseconds);
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
        public void processing_file_with_sdk_should_succeed(string fileId)
        {
            var s = new Stopwatch();
            s.Start();
            _unoConversion.Run(_mapping[fileId], "pdf");
            s.Stop();
            Debug.WriteLine("{0} conversion took {1} ms", fileId, s.ElapsedMilliseconds);
        }

        [Test]
        public void parallel_convesion_should_not_throw_exceptions()
        {
            Parallel.ForEach(
                _mapping.Keys,
//                new ParallelOptions() { MaxDegreeOfParallelism = 2 },
                k => _unoConversion.Run(_mapping[k], "pdf")
            );
        }
    }
}
