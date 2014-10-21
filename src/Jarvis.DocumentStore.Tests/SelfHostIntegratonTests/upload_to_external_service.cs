using System.Linq;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.SelfHostIntegratonTests
{
    [TestFixture, Explicit]
    public class upload_to_external_service
    {
        private DocumentStoreServiceClient _docs;
        private DocumentStoreServiceClient _demo;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _docs = new DocumentStoreServiceClient(
                TestConfig.ServerAddress, 
                TestConfig.DocsTenant
            );

            _demo = new DocumentStoreServiceClient(
                TestConfig.ServerAddress, 
                TestConfig.DemoTenant
            );
        }

        [Test]
        public void drop_all_tenants()
        {
            MongoDbTestConnectionProvider.DropAll();
        }

        [Test]
        public void upload_pdf()
        {
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, "Rev_1").Wait();
        }

        [Test]
        public void upload_pdf_to_demo_and_docs_tenants()
        {
            Task.WaitAll(
                _docs.UploadAsync(TestConfig.PathToDocumentPdf, "Rev_1"),
                _demo.UploadAsync(TestConfig.PathToDocumentPdf, "Rev_1")
            );
        }

        [Test]
        public void upload_same_pdf_with_two_handles()
        {
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, "Pdf_1").Wait();
            _docs.UploadAsync(TestConfig.PathToDocumentPdf, "Pdf_2").Wait();
        }

        [Test]
        public void upload_same_pdf_100_times_with_unique_handle()
        {
            var uploads = Enumerable
                .Range(1, 100)
                .Select(x => _docs.UploadAsync(TestConfig.PathToDocumentPdf, "Rev_" + x))
                .ToArray();

            Task.WaitAll(uploads);
        }


        [Test]
        public void upload_doc()
        {
            _docs.UploadAsync(TestConfig.PathToWordDocument, "doc").Wait();
        }

        [Test]
        public void upload_same_doc_100_times_with_unique_handle()
        {
            var uploads = Enumerable
                .Range(1, 100)
                .Select(x => _docs.UploadAsync(TestConfig.PathToWordDocument, "doc_" + x))
                .ToArray();

            Task.WaitAll(uploads);
        }

        [Test]
        public void upload_pdf_with_handleA_and_handleB()
        {
            Task.WaitAll(
                _docs.UploadAsync(TestConfig.PathToDocumentCopyPdf, "a"),
                _docs.UploadAsync(TestConfig.PathToDocumentPdf, "b")
            );
        }

        [Test]
        public void upload_html()
        {
            _docs.UploadAsync(TestConfig.PathToHtml, "html").Wait();
        }

        [Test]
        public void upload_excel()
        {
            _docs.UploadAsync(TestConfig.PathToExcelDocument, "xlsx").Wait();
        }

        [Test]
        public void upload_ppt()
        {
            _docs.UploadAsync(TestConfig.PathToPowerpointDocument, "pptx").Wait();
        }

        [Test]
        public void upload_pps()
        {
            _docs.UploadAsync(TestConfig.PathToPowerpointShow, "ppsx").Wait();
        }

        [Test]
        public void upload_odt()
        {
            _docs.UploadAsync(TestConfig.PathToOpenDocumentText, "odt").Wait();
        }

        [Test]
        public void upload_ods()
        {
            _docs.UploadAsync(TestConfig.PathToOpenDocumentSpreadsheet, "ods").Wait();
        }

        [Test]
        public void upload_odp()
        {
            _docs.UploadAsync(TestConfig.PathToOpenDocumentPresentation, "odp").Wait();
        }

        [Test]
        public void upload_rtf()
        {
            _docs.UploadAsync(TestConfig.PathToRTFDocument, "rtf").Wait();
        }

        [Test]
        public void upload_msg()
        {
            _docs.UploadAsync(TestConfig.PathToMsg, "outlook_1").Wait();
        }

        [Test]
        public void upload_eml()
        {
            _docs.UploadAsync(TestConfig.PathToEml, "eml_1").Wait();
        }

        [Test]
        public void upload_medium_jpg()
        {
            _docs.UploadAsync(TestConfig.PathToMediumJpg, "jpg_1").Wait();
        }

        [Test]
        public void upload_multi()
        {
            Task.WaitAll(
                _docs.UploadAsync(TestConfig.PathToWordDocument, "docx"),
                _docs.UploadAsync(TestConfig.PathToExcelDocument, "xlsx"),
                _docs.UploadAsync(TestConfig.PathToPowerpointDocument, "pptx"),
                _docs.UploadAsync(TestConfig.PathToPowerpointShow, "ppsx"),
                _docs.UploadAsync(TestConfig.PathToOpenDocumentText, "odt"),
                _docs.UploadAsync(TestConfig.PathToOpenDocumentSpreadsheet, "ods"),
                _docs.UploadAsync(TestConfig.PathToOpenDocumentPresentation, "odp"),
                _docs.UploadAsync(TestConfig.PathToRTFDocument, "rtf"),
                _docs.UploadAsync(TestConfig.PathToHtml, "html")
            );
        }
    }
}