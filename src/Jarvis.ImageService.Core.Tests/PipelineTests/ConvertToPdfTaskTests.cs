using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.ImageService.Core.Model;
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
