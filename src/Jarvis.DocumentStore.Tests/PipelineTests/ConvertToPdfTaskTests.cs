using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ProcessingPipeline.Conversions;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Tests.Support;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.PipelineTests
{
    [TestFixture (Category = "integration")]
    public class ConvertToPdfTaskTests
    {
        GridFSFileStore _fileStore;
        LibreOfficeConversion _withLibreOfficeConversion;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            MongoDbTestConnectionProvider.TestDb.Drop();

            _fileStore = new GridFSFileStore(MongoDbTestConnectionProvider.TestDb);
            _fileStore.Upload(new FileId("docx"), TestConfig.PathToWordDocument);
            _fileStore.Upload(new FileId("xlsx"), TestConfig.PathToExcelDocument);
            _fileStore.Upload(new FileId("pptx"), TestConfig.PathToPowerpointDocument);
            _fileStore.Upload(new FileId("ppsx"), TestConfig.PathToPowerpointShow);
            _fileStore.Upload(new FileId("txt"), TestConfig.PathToTextDocument);
            _fileStore.Upload(new FileId("odt"), TestConfig.PathToOpenDocumentText);
            _fileStore.Upload(new FileId("ods"), TestConfig.PathToOpenDocumentSpreadsheet);
            _fileStore.Upload(new FileId("odp"), TestConfig.PathToOpenDocumentPresentation);
            _fileStore.Upload(new FileId("rtf"), TestConfig.PathToRTFDocument);

            _withLibreOfficeConversion = new LibreOfficeConversion(_fileStore, new ConfigService())
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
            _withLibreOfficeConversion.Run(new FileId(fileId), "pdf");
            Assert.AreEqual("application/pdf", _fileStore.GetDescriptor(new FileId(fileId)).ContentType);
        }
    }
}
