using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Shared.Model;
using Jarvis.DocumentStore.Tests.JobTests;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using Jarvis.Framework.Kernel.MultitenantSupport;
using Jarvis.Framework.Kernel.ProjectionEngine;
using Jarvis.Framework.Shared.MultitenantSupport;
using Jarvis.Framework.Shared.ReadModel;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NUnit.Framework;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using DocumentFormat = Jarvis.DocumentStore.Client.Model.DocumentFormat;
using System;
using System.Net;
using Newtonsoft.Json;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Shared.Jobs;
using Jarvis.DocumentStore.Tests.ProjectionTests;
using Jarvis.Framework.Kernel.ProjectionEngine.RecycleBin;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using NSubstitute;
using DocumentHandle = Jarvis.DocumentStore.Client.Model.DocumentHandle;

// ReSharper disable InconsistentNaming
namespace Jarvis.DocumentStore.Tests.SelfHostIntegratonTests
{
    [TestFixture]
    public class DocumentControllerIntegrationTests
    {
        DocumentStoreBootstrapper _documentStoreService;
        private DocumentStoreServiceClient _documentStoreClient;
        private MongoCollection<DocumentDescriptorReadModel> _documentDescriptorCollection;
        private MongoCollection<DocumentReadModel> _documentCollection;
        private MongoCollection<StreamReadModel> _streamCollection;
        private ITriggerProjectionsUpdate _projections;
        private ITenant _tenant;

        private async Task UpdateAndWaitAsync()
        {
            await _projections.UpdateAndWait();
        }

        [SetUp]
        public void SetUp()
        {
            var config = new DocumentStoreTestConfiguration();
            MongoDbTestConnectionProvider.DropTestsTenant();
            config.SetTestAddress(TestConfig.ServerAddress);
            _documentStoreService = new DocumentStoreBootstrapper();
            _documentStoreService.Start(config);
            _documentStoreClient = new DocumentStoreServiceClient(
                TestConfig.ServerAddress,
                TestConfig.Tenant
            );
            _tenant = ContainerAccessor.Instance.Resolve<TenantManager>().Current;
            _projections = _tenant.Container.Resolve<ITriggerProjectionsUpdate>();
            _documentDescriptorCollection = MongoDbTestConnectionProvider.ReadModelDb.GetCollection<DocumentDescriptorReadModel>("rm.DocumentDescriptor");
            _documentCollection = MongoDbTestConnectionProvider.ReadModelDb.GetCollection<DocumentReadModel>("rm.Document");
            _streamCollection = MongoDbTestConnectionProvider.ReadModelDb.GetCollection<StreamReadModel>("rm.Stream");
        }

        [TearDown]
        public void TearDown()
        {
            _documentStoreService.Stop();
            BsonClassMapHelper.Clear();
        }

        [Test]
        public async void should_get_info_without_content()
        {
            var documentHandle = DocumentHandle.FromString("Pdf_2");

            await _documentStoreClient.UploadAsync(
                TestConfig.PathToDocumentPdf,
                documentHandle
            );

            // waits for storage
            await UpdateAndWaitAsync();
            var format = new DocumentFormat("original");

            var options = new OpenOptions()
            {
                FileName = "pluto.pdf",
                SkipContent = true
            };

            var reader = _documentStoreClient.OpenRead(documentHandle, format, options);
            using (var downloaded = new MemoryStream())
            {
                await (await reader.OpenStream()).CopyToAsync(downloaded);
                Assert.AreEqual(0, downloaded.Length);
                Assert.AreEqual(72768, reader.ContentLength);
            }
        }

        [Test]
        public async void should_download_with_range_header()
        {
            var documentHandle = DocumentHandle.FromString("Pdf_2");

            await _documentStoreClient.UploadAsync(
                TestConfig.PathToDocumentPdf,
                documentHandle
            );

            // waits for storage
            await UpdateAndWaitAsync();
            var format = new DocumentFormat("original");

            var options = new OpenOptions()
            {
                FileName = "pluto.pdf",
                RangeFrom = 0,
                RangeTo = 199
            };

            var reader = _documentStoreClient.OpenRead(documentHandle, format, options);
            using (var downloaded = new MemoryStream())
            {
                await (await reader.OpenStream()).CopyToAsync(downloaded);

                Assert.AreEqual(200, downloaded.Length, "Wrong range support");
                Assert.AreEqual(200, reader.ContentLength);
                Assert.AreEqual("bytes 0-199/72768", reader.ReponseHeaders[HttpResponseHeader.ContentRange]);
            }

            //load without rangeto
            options = new OpenOptions()
            {
                FileName = "pluto.pdf",
                RangeFrom = 200
            };

            reader = _documentStoreClient.OpenRead(documentHandle, format, options);
            using (var downloaded = new MemoryStream())
            {
                await (await reader.OpenStream()).CopyToAsync(downloaded);
                Assert.AreEqual(72768 - 200, downloaded.Length, "Wrong range support");
                Assert.AreEqual(72768 - 200, reader.ContentLength);
                Assert.AreEqual("bytes 200-72767/72768", reader.ReponseHeaders[HttpResponseHeader.ContentRange]);
            }
        }

        [Test]
        public async void should_upload_and_download_original_format()
        {
            await _documentStoreClient.UploadAsync(
                TestConfig.PathToDocumentPdf,
                DocumentHandle.FromString("Pdf_2"),
                new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            // waits for storage
            await UpdateAndWaitAsync();

            var reader = _documentStoreClient.OpenRead(DocumentHandle.FromString("Pdf_2"));
            using (var downloaded = new MemoryStream())
            using (var uploaded = new MemoryStream())
            {
                using (var fileStream = File.OpenRead(TestConfig.PathToDocumentPdf))
                {
                    await fileStream.CopyToAsync(uploaded);
                }
                await (await reader.OpenStream()).CopyToAsync(downloaded);

                Assert.IsTrue(CompareMemoryStreams(uploaded, downloaded));
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
                DocumentHandle.FromString("Pdf_1"),
                new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            Assert.AreEqual("8fe8386418f85ef4ee8ef1f3f1117928", response.Hash);
            Assert.AreEqual("md5", response.HashType);
            Assert.AreEqual("http://localhost:5123/tests/documents/pdf_1", response.Uri);
            // wait background projection polling
            await UpdateAndWaitAsync();

            var customData = await _documentStoreClient.GetCustomDataAsync(DocumentHandle.FromString("Pdf_1"));
            Assert.NotNull(customData);
            Assert.IsTrue(customData.ContainsKey("callback"));
            Assert.AreEqual("http://localhost/demo", customData["callback"]);
        }

        [Test]
        public async void should_upload_with_a_stream()
        {
            var handle = DocumentHandle.FromString("Pdf_4");

            using (var stream = File.OpenRead(TestConfig.PathToDocumentPdf))
            {
                var response = await _documentStoreClient.UploadAsync(
                    "demo.pdf",
                    handle,
                    stream
                );

                Assert.AreEqual("8fe8386418f85ef4ee8ef1f3f1117928", response.Hash);
                Assert.AreEqual("md5", response.HashType);
                Assert.AreEqual("http://localhost:5123/tests/documents/pdf_4", response.Uri);
            }
        }

        [Test]
        public async void should_get_document_formats()
        {
            var handle = new DocumentHandle("Formats");
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, handle);

            // wait background projection polling
            await UpdateAndWaitAsync();
            var formats = await _documentStoreClient.GetFormatsAsync(handle);
            Assert.NotNull(formats);
            Assert.IsTrue(formats.HasFormat(new DocumentFormat("original")));
        }



        [Test]
        public async void can_add_new_format_with_api()
        {
            //Upload original
            var handle = new DocumentHandle("Add_Format_Test");
            await _documentStoreClient.UploadAsync(TestConfig.PathToOpenDocumentText, handle);

            // wait background projection polling
            await UpdateAndWaitAsync();

            //now add format to document.
            AddFormatFromFileToDocumentModel model = new AddFormatFromFileToDocumentModel();
            model.DocumentHandle = handle;
            model.PathToFile = TestConfig.PathToTextDocument;
            model.CreatedById = "tika";
            model.Format = new DocumentFormat("tika");
            await _documentStoreClient.AddFormatToDocument(model, new Dictionary<String, Object>());

            // wait background projection polling
            await UpdateAndWaitAsync();
            var formats = await _documentStoreClient.GetFormatsAsync(handle);
            Assert.NotNull(formats);
            Assert.IsTrue(formats.HasFormat(new DocumentFormat("original")));
            Assert.IsTrue(formats.HasFormat(new DocumentFormat("tika")));
            Assert.That(formats, Has.Count.EqualTo(2));
        }

        [Test]
        public async void can_add_new_format_with_api_and_automatic_format_detection()
        {
            //Upload original
            var handle = new DocumentHandle("Add_Format_Test");
            await _documentStoreClient.UploadAsync(TestConfig.PathToOpenDocumentText, handle);

            // wait background projection polling
            await UpdateAndWaitAsync();

            //now add format to document.
            AddFormatFromFileToDocumentModel model = new AddFormatFromFileToDocumentModel();
            model.DocumentHandle = handle;
            model.PathToFile = TestConfig.PathToDocumentPdf;
            model.CreatedById = "office";
            model.Format = null; //NO FORMAT, I want document store to be able to detect format
            await _documentStoreClient.AddFormatToDocument(model, new Dictionary<String, Object>());

            // wait background projection polling
            await UpdateAndWaitAsync();
            var formats = await _documentStoreClient.GetFormatsAsync(handle);
            Assert.NotNull(formats);
            Assert.IsTrue(formats.HasFormat(new DocumentFormat("original")));
            Assert.IsTrue(formats.HasFormat(new DocumentFormat("pdf")));
            Assert.That(formats, Has.Count.EqualTo(2));
        }

        [Test]
        public async void can_add_new_format_with_api_from_object()
        {
            //Upload original
            var handle = new DocumentHandle("Add_Format_Test");
            await _documentStoreClient.UploadAsync(TestConfig.PathToOpenDocumentText, handle);

            // wait background projection polling
            await UpdateAndWaitAsync();

            DocumentContent content = new DocumentContent(new DocumentContent.DocumentPage[]
            {
                new DocumentContent.DocumentPage(1, "TEST"), 
            }, new DocumentContent.MetadataHeader[] { });
            //now add format to document.
            AddFormatFromObjectToDocumentModel model = new AddFormatFromObjectToDocumentModel();
            model.DocumentHandle = handle;
            model.StringContent = JsonConvert.SerializeObject(content);
            model.CreatedById = "tika";
            model.FileName = "add_format_test.content";
            model.Format = new DocumentFormat("content");
            await _documentStoreClient.AddFormatToDocument(model, new Dictionary<String, Object>());

            // wait background projection polling
            await UpdateAndWaitAsync();
            var formats = await _documentStoreClient.GetFormatsAsync(handle);
            Assert.NotNull(formats);
            Assert.IsTrue(formats.HasFormat(new DocumentFormat("original")));
            Assert.IsTrue(formats.HasFormat(new DocumentFormat("content")));
            Assert.That(formats, Has.Count.EqualTo(2));

            await CompareDownloadedStreamToStringContent(
                model.StringContent,
                _documentStoreClient.OpenRead(handle, new DocumentFormat("content")));

        }

        [Test]
        public async void adding_two_time_same_format_overwrite_older()
        {
            //Upload original
            var handle = new DocumentHandle("Add_Format_Test");
            await _documentStoreClient.UploadAsync(TestConfig.PathToOpenDocumentText, handle);

            // wait background projection polling
            await UpdateAndWaitAsync();

            //now add format to document.
            AddFormatFromFileToDocumentModel model = new AddFormatFromFileToDocumentModel();
            model.DocumentHandle = handle;
            model.PathToFile = TestConfig.PathToTextDocument;
            model.CreatedById = "tika";
            model.Format = new DocumentFormat("tika");
            await _documentStoreClient.AddFormatToDocument(model, new Dictionary<String, Object>());

            // wait background projection polling
            await UpdateAndWaitAsync();

            //now add same format with different content.
            model = new AddFormatFromFileToDocumentModel();
            model.DocumentHandle = handle;
            model.PathToFile = TestConfig.PathToHtml;
            model.CreatedById = "tika";
            model.Format = new DocumentFormat("tika");
            await _documentStoreClient.AddFormatToDocument(model, new Dictionary<String, Object>());

            // wait background projection polling
            await UpdateAndWaitAsync();
            var formats = await _documentStoreClient.GetFormatsAsync(handle);
            Assert.NotNull(formats);
            Assert.IsTrue(formats.HasFormat(new DocumentFormat("original")));
            Assert.IsTrue(formats.HasFormat(new DocumentFormat("tika")));
            Assert.That(formats, Has.Count.EqualTo(2));

            await CompareDownloadedStreamToFile(TestConfig.PathToHtml, _documentStoreClient.OpenRead(handle, new DocumentFormat("tika")));
        }


        [Test]
        public async void can_add_attachment_to_existing_handle()
        {
            //Upload father
            var fatherHandle = new DocumentHandle("father");
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, fatherHandle);

            // wait background projection polling
            await UpdateAndWaitAsync();

            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToDocumentPng, fatherHandle, "Content", Path.GetFileName(TestConfig.PathToDocumentPng));

            // wait background projection polling
            await UpdateAndWaitAsync();

            var document = _documentDescriptorCollection.Find(Query.EQ("Documents", "content_1")).SingleOrDefault();
            Assert.That(document, Is.Not.Null, "Document with child handle was not find.");

            var handle = _documentDescriptorCollection.Find(Query.EQ("Documents", "father")).SingleOrDefault();
            Assert.That(handle, Is.Not.Null, "Father Handle Not Find");
            Assert.That(handle.Attachments.Select(a => a.Handle), Is.EquivalentTo(new[] { 
                new Jarvis.DocumentStore.Core.Model.DocumentHandle("content_1")
            }));
        }

        [Test]
        public async void add_multiple_attachment_to_existing_handle()
        {
            //Upload father
            var fatherHandle = new DocumentHandle("father");
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, fatherHandle);
            await UpdateAndWaitAsync();

            //upload attachments
            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToDocumentPng, fatherHandle, "Content", Path.GetFileName(TestConfig.PathToDocumentPng));
            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToOpenDocumentText, fatherHandle, "Content", Path.GetFileName(TestConfig.PathToOpenDocumentText));
            await UpdateAndWaitAsync();

            var document = _documentDescriptorCollection.Find(Query.EQ("Documents", "content_1")).SingleOrDefault();
            Assert.That(document, Is.Not.Null, "Document with first child handle was not find.");

            document = _documentDescriptorCollection.Find(Query.EQ("Documents", "content_2")).SingleOrDefault();
            Assert.That(document, Is.Not.Null, "Document with second child handle was not find.");

            var handle = _documentDescriptorCollection.Find(Query.EQ("Documents", "father")).SingleOrDefault();
            Assert.That(handle, Is.Not.Null, "Father Handle Not Find");
            Assert.That(handle.Attachments.Select(a => a.Handle), Is.EquivalentTo(new[] { new Core.Model.DocumentHandle("content_1"), new Core.Model.DocumentHandle("content_2") }));
        }

        //Delete by source type is not anymore supported
        //[Test]
        //public async void add_multiple_attachment_to_existing_handle_then_delete_by_source()
        //{
        //    //Upload father
        //    var fatherHandle = new DocumentHandle("father");
        //    await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, fatherHandle);
        //    await UpdateAndWaitAsync();

        //    //upload attachments
        //    await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToDocumentPng, fatherHandle, "sourcea", Path.GetFileName(TestConfig.PathToDocumentPng));
        //    await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToOpenDocumentText, fatherHandle, "sourceb", Path.GetFileName(TestConfig.PathToOpenDocumentText));
        //    await UpdateAndWaitAsync();

        //    await _documentStoreClient.DeleteAttachmentsAsync(fatherHandle, "sourceb");
        //    await UpdateAndWaitAsync();

        //    var handle = _documentCollection.Find(Query.EQ("_id", "sourcea_1")).SingleOrDefault();
        //    Assert.That(handle, Is.Not.Null, "SourceA attachment should not be deleted.");

        //    handle = _documentCollection.Find(Query.EQ("_id", "sourceb_1")).SingleOrDefault();
        //    Assert.That(handle, Is.Null, "SourceB attachment should be deleted.");
        //}

        [Test]
        public async void add_multiple_attachment_to_existing_handle_then_delete_handle()
        {
            //Upload father
            var fatherHandle = new DocumentHandle("father");
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, fatherHandle);
            await UpdateAndWaitAsync();

            //upload attachments
            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToDocumentPng, fatherHandle, "Zip", Path.GetFileName(TestConfig.PathToDocumentPng));
            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToOpenDocumentText, fatherHandle, "Zip", Path.GetFileName(TestConfig.PathToOpenDocumentText));
            await UpdateAndWaitAsync();

            await _documentStoreClient.DeleteAsync(fatherHandle);
            await UpdateAndWaitAsync();

            Assert.That(_documentDescriptorCollection.Count(), Is.EqualTo(0), "Attachment should be deleted.");
            Assert.That(_documentCollection.Count(), Is.EqualTo(0), "Attachment should be deleted.");


        }

        [Test]
        public async void can_retrieve_attachments()
        {
            //Upload father
            var fatherHandle = new DocumentHandle("father");
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, fatherHandle);

            // wait background projection polling
            await UpdateAndWaitAsync();

            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToDocumentPng, fatherHandle, "source", Path.GetFileName(TestConfig.PathToDocumentPng));

            // wait background projection polling
            await UpdateAndWaitAsync();

            var attachments = await _documentStoreClient.GetAttachmentsAsync(fatherHandle);
            Assert.NotNull(attachments);
            Assert.That(attachments.Attachments.Length, Is.EqualTo(1));
            Assert.That(attachments.Attachments[0].RelativePath.ToString(), Is.EqualTo(Path.GetFileName(TestConfig.PathToDocumentPng)));
            Assert.That(attachments.Attachments[0].Handle.ToString(), Is.EqualTo("http://localhost:5123/tests/documents/source_1"));
        }

        [Test]
        public async void verify_de_duplication_delete_original_blob()
        {
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, new DocumentHandle("handleA"));
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, new DocumentHandle("handleB"));
            // wait background projection polling
            await UpdateAndWaitAsync();

            //now we need to wait cleanupJobs to start 
            var store = _tenant.Container.Resolve<IBlobStore>();
            CleanupJob job = _tenant.Container.Resolve<CleanupJob>();
            IJobExecutionContext context = NSubstitute.Substitute.For<IJobExecutionContext>();
            IJobDetail jobDetail = NSubstitute.Substitute.For<IJobDetail>();
            IDictionary<string, object> mapd = new Dictionary<string, object>() { { JobKeys.TenantId.ToString(), _tenant.Id.ToString() } };
            JobDataMap map = new JobDataMap(mapd);
            jobDetail.JobDataMap.Returns(map);
            context.JobDetail.Returns(jobDetail);
            job.Execute(context);

            //verify that blob
            Assert.That(store.GetDescriptor(new BlobId("original.1")), Is.Not.Null);

            Assert.Throws<Exception>(() => store.GetDescriptor(new BlobId("original.2")));
        }

        [Test]
        public async void attachments_not_retrieve_nested_attachment()
        {
            //Upload father
            var fatherHandle = new DocumentHandle("father");
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, fatherHandle);

            // wait background projection polling
            await UpdateAndWaitAsync();

            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToDocumentPng, fatherHandle, "source", Path.GetFileName(TestConfig.PathToDocumentPng));

            // wait background projection polling
            await UpdateAndWaitAsync();

            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToDocumentPng, new DocumentHandle("source_1"), "nested", Path.GetFileName(TestConfig.PathToDocumentPng));

            // wait background projection polling
            await UpdateAndWaitAsync();

            var attachments = await _documentStoreClient.GetAttachmentsAsync(fatherHandle);
            Assert.NotNull(attachments);
            Assert.That(attachments.Attachments.Length, Is.EqualTo(1));
            Assert.That(attachments.Attachments[0].RelativePath.ToString(), Is.EqualTo(Path.GetFileName(TestConfig.PathToDocumentPng)));
            Assert.That(attachments.Attachments[0].Handle.ToString(), Is.EqualTo("http://localhost:5123/tests/documents/source_1"));
        }

        [Test]
        public async void attachments_fat_retrieve_nested_attachment()
        {
            //Upload father
            var fatherHandle = new DocumentHandle("father");
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, fatherHandle);

            // wait background projection polling
            await UpdateAndWaitAsync();

            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToDocumentPng, fatherHandle, "source", Path.GetFileName(TestConfig.PathToDocumentPng));

            // wait background projection polling
            await UpdateAndWaitAsync();

            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToExcelDocument, new DocumentHandle("source_1"), "nested", Path.GetFileName(TestConfig.PathToExcelDocument));

            // wait background projection polling
            await UpdateAndWaitAsync();

            var attachments = await _documentStoreClient.GetAttachmentsFatAsync(fatherHandle);
            Assert.NotNull(attachments);
            Assert.That(attachments.Attachments, Has.Count.EqualTo(2));
            Assert.That(attachments.Attachments.Select(a => a.FileName), Is.EquivalentTo(new[] {
                Path.GetFileName(TestConfig.PathToDocumentPng), 
                Path.GetFileName(TestConfig.PathToExcelDocument)
            }));
            Assert.That(attachments.Attachments.Select(a => a.Uri),
                Is.EquivalentTo(new[] {
                    new Uri("http://localhost:5123/tests/documents/source_1"), 
                    new Uri("http://localhost:5123/tests/documents/nested_1")
            }));
        }

        private async Task CompareDownloadedStreamToFile(string pathToFileToCompare, DocumentFormatReader documentFormatReader)
        {
            using (var downloaded = new MemoryStream())
            using (var uploaded = new MemoryStream())
            {
                using (var fileStream = File.OpenRead(pathToFileToCompare))
                {
                    await fileStream.CopyToAsync(uploaded);
                }
                await (await documentFormatReader.OpenStream()).CopyToAsync(downloaded);

                Assert.IsTrue(CompareMemoryStreams(uploaded, downloaded),
                    "Downloaded format is not equal to last format uploaded");
            }
        }

        private async Task CompareDownloadedStreamToStringContent(string contentToCompare, DocumentFormatReader documentFormatReader)
        {
            using (var downloaded = new MemoryStream())
            {
                await (await documentFormatReader.OpenStream()).CopyToAsync(downloaded);
                var downloadedString = System.Text.Encoding.UTF8.GetString(downloaded.ToArray());
                Assert.AreEqual(downloadedString, contentToCompare, "Downloaded stream is not identical");
            }
        }

        [Test]
        public async void should_upload_get_metadata_and_delete_a_document()
        {
            var handle = DocumentHandle.FromString("Pdf_3");

            await _documentStoreClient.UploadAsync(
                TestConfig.PathToDocumentPdf,
                handle,
                new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            // wait background projection polling
            await UpdateAndWaitAsync();

            var data = await _documentStoreClient.GetCustomDataAsync(handle);

            await _documentStoreClient.DeleteAsync(handle);

            await UpdateAndWaitAsync();

            var ex = Assert.Throws<HttpRequestException>(async () =>
            {
                await _documentStoreClient.GetCustomDataAsync(handle);
            });

            Assert.IsTrue(ex.Message.Contains("404"));

            // check readmodel
            var tenantAccessor = ContainerAccessor.Instance.Resolve<ITenantAccessor>();
            var tenant = tenantAccessor.GetTenant(new TenantId(TestConfig.Tenant));
            var docReader = tenant.Container.Resolve<IMongoDbReader<DocumentDescriptorReadModel, DocumentDescriptorId>>();

            var allDocuments = docReader.AllUnsorted.Count();
            Assert.AreEqual(0, allDocuments);
        }

        //        [Test, Explicit]
        public void should_upload_all_documents()
        {
            Task.WaitAll(
                _documentStoreClient.UploadAsync(TestConfig.PathToWordDocument, DocumentHandle.FromString("docx")),
                _documentStoreClient.UploadAsync(TestConfig.PathToExcelDocument, DocumentHandle.FromString("xlsx")),
                _documentStoreClient.UploadAsync(TestConfig.PathToPowerpointDocument, DocumentHandle.FromString("pptx")),
                _documentStoreClient.UploadAsync(TestConfig.PathToPowerpointShow, DocumentHandle.FromString("ppsx")),
                _documentStoreClient.UploadAsync(TestConfig.PathToOpenDocumentText, DocumentHandle.FromString("odt")),
                _documentStoreClient.UploadAsync(TestConfig.PathToOpenDocumentSpreadsheet, DocumentHandle.FromString("ods")),
                _documentStoreClient.UploadAsync(TestConfig.PathToOpenDocumentPresentation, DocumentHandle.FromString("odp")),
                _documentStoreClient.UploadAsync(TestConfig.PathToRTFDocument, DocumentHandle.FromString("rtf")),
                _documentStoreClient.UploadAsync(TestConfig.PathToHtml, DocumentHandle.FromString("html"))
            );

            Debug.WriteLine("Done");
        }


    }
}
