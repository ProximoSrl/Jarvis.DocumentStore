using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.ImageService.Core.ProcessingPipeline.Conversions;
using Jarvis.ImageService.Core.Services;
using Jarvis.ImageService.Core.Storage;
using Jarvis.ImageService.Core.Tests.Support;
using NUnit.Framework;

namespace Jarvis.ImageService.Core.Tests.PipelineTests
{
    [TestFixture (Category = "integration")]
    public class ConvertToPdfTaskTests
    {
        GridFSFileStore _fileStore;
        ConvertFileToPdfWithLibreOfficeTask _withLibreOfficeTask;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            MongoDbTestConnectionProvider.TestDb.Drop();

            _fileStore = new GridFSFileStore(MongoDbTestConnectionProvider.TestDb);
            _fileStore.Upload("docx", TestConfig.PathToWordDocument);
            _fileStore.Upload("xlsx", TestConfig.PathToExcelDocument);
            _fileStore.Upload("pptx", TestConfig.PathToPowerpointDocument);
            _fileStore.Upload("txt", TestConfig.PathToTextDocument);
            _fileStore.Upload("odt", TestConfig.PathToOpenDocumentText);
            _fileStore.Upload("ods", TestConfig.PathToOpenDocumentSpreadsheet);
            _fileStore.Upload("odp", TestConfig.PathToOpenDocumentPresentation);
            _fileStore.Upload("rtf", TestConfig.PathToRTFDocument);

            _withLibreOfficeTask = new ConvertFileToPdfWithLibreOfficeTask(_fileStore, new ConfigService())
            {
                Logger = new ConsoleLogger()
            };
        }

        [Test]
        [TestCase("docx")]
        [TestCase("xlsx")]
        [TestCase("pptx")]
        [TestCase("txt")]
        [TestCase("odt")]
        [TestCase("ods")]
        [TestCase("odp")]
        [TestCase("rtf")]
        public void processing_file_should_succeed(string fileId)
        {
            _withLibreOfficeTask.Convert(fileId);
            Assert.AreEqual("application/pdf", _fileStore.GetDescriptor(fileId).ContentType);
        }
    }
}
