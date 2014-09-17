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
        ConvertToPdfTask _task;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            MongoDbTestConnectionProvider.TestDb.Drop();

            _fileStore = new GridFSFileStore(MongoDbTestConnectionProvider.TestDb);
            _fileStore.Upload("docx", SampleData.PathToWordDocument);
            _fileStore.Upload("xlsx", SampleData.PathToExcelDocument);
            _fileStore.Upload("pptx", SampleData.PathToPowerpointDocument);
            _fileStore.Upload("txt", SampleData.PathToTextDocument);
            _fileStore.Upload("odt", SampleData.PathToOpenDocumentText);
            _fileStore.Upload("ods", SampleData.PathToOpenDocumentSpreadsheet);
            _fileStore.Upload("odp", SampleData.PathToOpenDocumentPresentation);

            _task = new ConvertToPdfTask(_fileStore, new ConfigService())
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
        public void processing_file_should_succeed(string fileId)
        {
            _task.Convert(fileId);
            Assert.AreEqual("application/pdf", _fileStore.GetDescriptor(fileId).ContentType);
        }
    }
}
