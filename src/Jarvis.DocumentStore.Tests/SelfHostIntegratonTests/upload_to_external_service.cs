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
            _client.UploadAsync(TestConfig.PathToDocumentPdf, "Rev_1").Wait();
        }

        [Test]
        public void upload_same_pdf_with_two_handles()
        {
            _client.UploadAsync(TestConfig.PathToDocumentPdf, "Pdf_1").Wait();
            _client.UploadAsync(TestConfig.PathToDocumentPdf, "Pdf_2").Wait();
        }

        [Test]
        public void upload_same_pdf_100_times_with_unique_handle()
        {
            var uploads = Enumerable
                .Range(1, 100)
                .Select(x => _client.UploadAsync(TestConfig.PathToDocumentPdf, "Rev_" + x))
                .ToArray();

            Task.WaitAll(uploads);
        }


        [Test]
        public void upload_doc()
        {
            _client.UploadAsync(TestConfig.PathToWordDocument, "doc").Wait();
        }

        [Test]
        public void upload_same_doc_100_times_with_unique_handle()
        {
            var uploads = Enumerable
                .Range(1, 100)
                .Select(x => _client.UploadAsync(TestConfig.PathToWordDocument, "doc_" + x))
                .ToArray();

            Task.WaitAll(uploads);
        }

        [Test]
        public void upload_pdf_with_handleA_and_handleB()
        {
            Task.WaitAll(
                _client.UploadAsync(TestConfig.PathToDocumentCopyPdf, "a"),
                _client.UploadAsync(TestConfig.PathToDocumentPdf, "b")
            );
        }

        [Test]
        public void upload_html()
        {
            _client.UploadAsync(TestConfig.PathToHtml, "html").Wait();
        }

        [Test]
        public void upload_excel()
        {
            _client.UploadAsync(TestConfig.PathToExcelDocument, "xlsx").Wait();
        }

        [Test]
        public void upload_ppt()
        {
            _client.UploadAsync(TestConfig.PathToPowerpointDocument, "pptx").Wait();
        }

        [Test]
        public void upload_pps()
        {
            _client.UploadAsync(TestConfig.PathToPowerpointShow, "ppsx").Wait();
        }

        [Test]
        public void upload_odt()
        {
            _client.UploadAsync(TestConfig.PathToOpenDocumentText, "odt").Wait();
        }

        [Test]
        public void upload_ods()
        {
            _client.UploadAsync(TestConfig.PathToOpenDocumentSpreadsheet, "ods").Wait();
        }

        [Test]
        public void upload_odp()
        {
            _client.UploadAsync(TestConfig.PathToOpenDocumentPresentation, "odp").Wait();
        }

        [Test]
        public void upload_rtf()
        {
            _client.UploadAsync(TestConfig.PathToRTFDocument, "rtf").Wait();
        }

        [Test]
        public void upload_msg()
        {
            _client.UploadAsync(TestConfig.PathToMsg, "outlook_1").Wait();
        }

        [Test]
        public void upload_eml()
        {
            _client.UploadAsync(TestConfig.PathToEml, "eml_1").Wait();
        }

        [Test]
        public void upload_medium_jpg()
        {
            _client.UploadAsync(TestConfig.PathToMediumJpg, "jpg_1").Wait();
        }

        [Test]
        public void upload_multi()
        {
            Task.WaitAll(
                _client.UploadAsync(TestConfig.PathToWordDocument, "docx"),
                _client.UploadAsync(TestConfig.PathToExcelDocument, "xlsx"),
                _client.UploadAsync(TestConfig.PathToPowerpointDocument, "pptx"),
                _client.UploadAsync(TestConfig.PathToPowerpointShow, "ppsx"),
                _client.UploadAsync(TestConfig.PathToOpenDocumentText, "odt"),
                _client.UploadAsync(TestConfig.PathToOpenDocumentSpreadsheet, "ods"),
                _client.UploadAsync(TestConfig.PathToOpenDocumentPresentation, "odp"),
                _client.UploadAsync(TestConfig.PathToRTFDocument, "rtf"),
                _client.UploadAsync(TestConfig.PathToHtml, "html")
            );
        }
    }
}