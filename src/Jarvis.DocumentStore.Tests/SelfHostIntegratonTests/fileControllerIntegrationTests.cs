using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            MongoDbTestConnectionProvider.TestDb.Drop();

            _documentStoreService = new DocumentStoreBootstrapper(TestConfig.ServerAddress);
            _documentStoreService.Start(config);
            _documentStoreClient = new DocumentStoreServiceClient(TestConfig.ServerAddress);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            _documentStoreService.Stop();
        }

        [Test]
        public async void should_upload_and_download_original_format()
        {
            await _documentStoreClient.Upload(
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
                using(var uploaded = new MemoryStream())
                {
                    using (var fileStream = File.OpenRead(TestConfig.PathToDocumentPdf))
                    {
                        await fileStream.CopyToAsync(uploaded);
                    }
                    await (await reader.ReadStream).CopyToAsync(downloaded);

                    Assert.IsTrue(CompareMemoryStreams(uploaded,downloaded));
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
            await _documentStoreClient.Upload(
                TestConfig.PathToDocumentPdf, 
                "Pdf_1", 
                new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            // wait background projection polling
            Thread.Sleep(500);

            var customData = await _documentStoreClient.GetCustomData("Pdf_1");
            Assert.NotNull(customData);
            Assert.IsTrue(customData.ContainsKey("callback"));
            Assert.AreEqual("http://localhost/demo", customData["callback"]);
        }

        [Test]
        public async void should_be_able_to_delete_a_document()
        {
            const string resourceId = "Pdf_3";
            await _documentStoreClient.Upload(
                TestConfig.PathToDocumentPdf,
                resourceId,
                new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            // wait background projection polling
            Thread.Sleep(500);

            var data = await _documentStoreClient.GetCustomData(resourceId);

            await _documentStoreClient.Delete(resourceId);

            Thread.Sleep(500);
            
            data = await _documentStoreClient.GetCustomData(resourceId);
        }

        [Test, Explicit]
        public void should_upload_all_documents()
        {
            Task.WaitAll(
                _documentStoreClient.Upload(TestConfig.PathToWordDocument, "docx"),
                _documentStoreClient.Upload(TestConfig.PathToExcelDocument, "xlsx"),
                _documentStoreClient.Upload(TestConfig.PathToPowerpointDocument, "pptx"),
                _documentStoreClient.Upload(TestConfig.PathToPowerpointShow, "ppsx"),
                _documentStoreClient.Upload(TestConfig.PathToOpenDocumentText, "odt"),
                _documentStoreClient.Upload(TestConfig.PathToOpenDocumentSpreadsheet, "ods"),
                _documentStoreClient.Upload(TestConfig.PathToOpenDocumentPresentation, "odp"),
                _documentStoreClient.Upload(TestConfig.PathToRTFDocument, "rtf"),
                _documentStoreClient.Upload(TestConfig.PathToHtml, "html")
            );

            Debug.WriteLine("Done");
        }
    }
}
