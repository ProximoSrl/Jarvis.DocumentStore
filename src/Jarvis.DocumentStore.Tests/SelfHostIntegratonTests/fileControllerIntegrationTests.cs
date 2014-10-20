using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using NUnit.Framework;

// ReSharper disable InconsistentNaming
namespace Jarvis.DocumentStore.Tests.SelfHostIntegratonTests
{
    [TestFixture]
    public class FileControllerIntegrationTests
    {
        DocumentStoreBootstrapper _documentStoreService;
        private DocumentStoreServiceClient _documentStoreClient;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var config = new DocumentStoreConfiguration(
                isApiServer: true,
                isWorker: false,
                isReadmodelBuilder: true
            );
            MongoDbTestConnectionProvider.DropTenant1();

            _documentStoreService = new DocumentStoreBootstrapper(TestConfig.ServerAddress);
            _documentStoreService.Start(config);
            _documentStoreClient = new DocumentStoreServiceClient(
                TestConfig.ServerAddress, 
                TestConfig.Tenant
            );
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            _documentStoreService.Stop();
        }

        [Test]
        public async void should_upload_and_download_original_format()
        {
            await _documentStoreClient.UploadAsync(
                TestConfig.PathToDocumentPdf,
                "Pdf_2",
                new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            // waits for storage
            Thread.Sleep(2000);

            using (var reader = _documentStoreClient.OpenRead("Pdf_2"))
            {
                using (var downloaded = new MemoryStream())
                using (var uploaded = new MemoryStream())
                {
                    using (var fileStream = File.OpenRead(TestConfig.PathToDocumentPdf))
                    {
                        await fileStream.CopyToAsync(uploaded);
                    }
                    await (await reader.ReadStream).CopyToAsync(downloaded);

                    Assert.IsTrue(CompareMemoryStreams(uploaded, downloaded));
                }
            }
        }

        private static bool CompareMemoryStreams(MemoryStream ms1, MemoryStream ms2)
        {
            if (ms1.Length != ms2.Length)
                return false;
            ms1.Position = 0;
            ms2.Position = 0;

            var msArray1 = ms1.ToArray();
            var msArray2 = ms2.ToArray();

            return msArray1.SequenceEqual(msArray2);
        }


        [Test]
        public async void should_upload_file_with_custom_data()
        {
            var response = await _documentStoreClient.UploadAsync(
                TestConfig.PathToDocumentPdf,
                "Pdf_1",
                new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            Assert.AreEqual("8fe8386418f85ef4ee8ef1f3f1117928", response.Hash);
            Assert.AreEqual("md5", response.HashType);
            Assert.AreEqual("http://localhost:5123/tests/documents/pdf_1", response.Uri);
            // wait background projection polling
            Thread.Sleep(500);

            var customData = await _documentStoreClient.GetCustomDataAsync("Pdf_1");
            Assert.NotNull(customData);
            Assert.IsTrue(customData.ContainsKey("callback"));
            Assert.AreEqual("http://localhost/demo", customData["callback"]);
        }

        [Test]
        public async void should_upload_get_metadata_and_delete_a_document()
        {
            const string resourceId = "Pdf_3";
            await _documentStoreClient.UploadAsync(
                TestConfig.PathToDocumentPdf,
                resourceId,
                new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            // wait background projection polling
            Thread.Sleep(500);

            var data = await _documentStoreClient.GetCustomDataAsync(resourceId);

            await _documentStoreClient.DeleteAsync(resourceId);

            Thread.Sleep(500);

            var ex = Assert.Throws<HttpRequestException>(async() =>
            {
                await _documentStoreClient.GetCustomDataAsync(resourceId);
            });

            Assert.IsTrue(ex.Message.Contains("404"));
        }

        [Test, Explicit]
        public void should_upload_all_documents()
        {
            Task.WaitAll(
                _documentStoreClient.UploadAsync(TestConfig.PathToWordDocument, "docx"),
                _documentStoreClient.UploadAsync(TestConfig.PathToExcelDocument, "xlsx"),
                _documentStoreClient.UploadAsync(TestConfig.PathToPowerpointDocument, "pptx"),
                _documentStoreClient.UploadAsync(TestConfig.PathToPowerpointShow, "ppsx"),
                _documentStoreClient.UploadAsync(TestConfig.PathToOpenDocumentText, "odt"),
                _documentStoreClient.UploadAsync(TestConfig.PathToOpenDocumentSpreadsheet, "ods"),
                _documentStoreClient.UploadAsync(TestConfig.PathToOpenDocumentPresentation, "odp"),
                _documentStoreClient.UploadAsync(TestConfig.PathToRTFDocument, "rtf"),
                _documentStoreClient.UploadAsync(TestConfig.PathToHtml, "html")
            );

            Debug.WriteLine("Done");
        }
    }
}
