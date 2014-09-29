using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Host.Support;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.PipelineTests
{
    [TestFixture, Explicit]
    public class ClientIntegrationTests
    {
        DocumentStoreBootstrapper _app;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _app = new DocumentStoreBootstrapper(TestConfig.ServerAddress);
            _app.Start();
            Thread.Sleep(3000);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            _app.Stop();
        }

        [Test]
        public void should_upload_all_documents()
        {
            var client = new DocumentStoreServiceClient(TestConfig.ServerAddress);

            Task.WaitAll(
                client.Upload(TestConfig.PathToWordDocument, "docx"),
                client.Upload(TestConfig.PathToExcelDocument, "xlsx"),
                client.Upload(TestConfig.PathToPowerpointDocument, "pptx"),
                client.Upload(TestConfig.PathToPowerpointShow, "ppsx"),
                client.Upload(TestConfig.PathToOpenDocumentText, "odt"),
                client.Upload(TestConfig.PathToOpenDocumentSpreadsheet, "ods"),
                client.Upload(TestConfig.PathToOpenDocumentPresentation, "odp"),
                client.Upload(TestConfig.PathToRTFDocument, "rtf"),
                client.Upload(TestConfig.PathToHtml, "html")
            );

            Debug.WriteLine("Done");
        }
    }

    [TestFixture, Explicit]
    public class upload_to_externa_service
    {
        [Test]
        public void upload_single()
        {
            var client = new DocumentStoreServiceClient(TestConfig.ServerAddress);
            client.Upload(TestConfig.PathToWordDocument, "doc").Wait();
        }

        [Test]
        public void upload_multi()
        {
            var client = new DocumentStoreServiceClient(TestConfig.ServerAddress);
            Task.WaitAll(
                client.Upload(TestConfig.PathToWordDocument, "docx"),
                client.Upload(TestConfig.PathToExcelDocument, "xlsx"),
                client.Upload(TestConfig.PathToPowerpointDocument, "pptx"),
                client.Upload(TestConfig.PathToPowerpointShow, "ppsx"),
                client.Upload(TestConfig.PathToOpenDocumentText, "odt"),
                client.Upload(TestConfig.PathToOpenDocumentSpreadsheet, "ods"),
                client.Upload(TestConfig.PathToOpenDocumentPresentation, "odp"),
                client.Upload(TestConfig.PathToRTFDocument, "rtf"),
                client.Upload(TestConfig.PathToHtml, "html")
            );
        }
    }
}
