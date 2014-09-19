using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.ImageService.Client;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.ProcessingPipeline.Conversions;
using Jarvis.ImageService.Core.Services;
using Jarvis.ImageService.Core.Storage;
using Jarvis.ImageService.Core.Tests.Support;
using NUnit.Framework;

namespace Jarvis.ImageService.Core.Tests.PipelineTests
{
    [TestFixture]
    public class ConvertHtmlToPdfTaskTests
    {
        GridFSFileStore _fileStore;

        [SetUp]
        public void SetUp()
        {
            MongoDbTestConnectionProvider.TestDb.Drop();
            _fileStore = new GridFSFileStore(MongoDbTestConnectionProvider.TestDb);

            var client = new ImageServiceClient(TestConfig.ServerAddress);
            var zipped = client.ZipHtmlPage(TestConfig.PathToHtml);
            _fileStore.Upload(new FileId("ziphtml"), zipped);
        }

        [Test]
        public void should_convert_htmlfolder_to_pdf()
        {
            var conversion = new HtmlToPdfConverter(_fileStore, new ConfigService())
            {
                Logger = new ConsoleLogger()
            };

            conversion.Run(new FileId("ziphtml"));

            var fi = _fileStore.GetDescriptor(new FileId("ziphtml"));
            Assert.AreEqual("application/pdf", fi.ContentType);
        }
    }
}
