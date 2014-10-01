using System.Linq;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.SelfHostIntegratonTests
{
    [TestFixture, Explicit]
    public class upload_to_external_service
    {
        private DocumentStoreServiceClient _client;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _client = new DocumentStoreServiceClient(TestConfig.ServerAddress);
        }

        [Test]
        public void upload_pdf()
        {
            _client.Upload(TestConfig.PathToDocumentPdf, "Rev_1").Wait();
        }

        [Test]
        public void upload_lots_of_pdf()
        {
            Parallel.ForEach(Enumerable.Range(1, 100), i =>
            {
                _client.Upload(TestConfig.PathToDocumentPdf, "Rev_"+i).Wait();
            });
        }


        [Test]
        public void upload_doc()
        {
            _client.Upload(TestConfig.PathToWordDocument, "doc").Wait();
        }        
        
        [Test]
        public void upload_html()
        {
            _client.Upload(TestConfig.PathToHtml, "html").Wait();
        }

        [Test]
        public void upload_excel()
        {
            _client.Upload(TestConfig.PathToExcelDocument, "xlsx").Wait();
        }      
        
        [Test]
        public void upload_ppt()
        {
            _client.Upload(TestConfig.PathToPowerpointDocument, "pptx").Wait();
        }

        [Test]
        public void upload_pps()
        {
            _client.Upload(TestConfig.PathToPowerpointShow, "ppsx").Wait();
        }

        [Test]
        public void upload_odt()
        {
            _client.Upload(TestConfig.PathToOpenDocumentText, "odt").Wait();
        }

        [Test]
        public void upload_ods()
        {
            _client.Upload(TestConfig.PathToOpenDocumentSpreadsheet, "ods").Wait();
        }

        [Test]
        public void upload_odp()
        {
            _client.Upload(TestConfig.PathToOpenDocumentPresentation, "odp").Wait();
        }

        [Test]
        public void upload_rtf()
        {
            _client.Upload(TestConfig.PathToRTFDocument, "rtf").Wait();
        }

        [Test]
        public void upload_multi()
        {
            Task.WaitAll(
                _client.Upload(TestConfig.PathToWordDocument, "docx"),
                _client.Upload(TestConfig.PathToExcelDocument, "xlsx"),
                _client.Upload(TestConfig.PathToPowerpointDocument, "pptx"),
                _client.Upload(TestConfig.PathToPowerpointShow, "ppsx"),
                _client.Upload(TestConfig.PathToOpenDocumentText, "odt"),
                _client.Upload(TestConfig.PathToOpenDocumentSpreadsheet, "ods"),
                _client.Upload(TestConfig.PathToOpenDocumentPresentation, "odp"),
                _client.Upload(TestConfig.PathToRTFDocument, "rtf"),
                _client.Upload(TestConfig.PathToHtml, "html")
            );
        }
    }
}