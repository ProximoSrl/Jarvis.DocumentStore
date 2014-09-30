using System.Threading.Tasks;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.SelfHostIntegratonTests
{
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