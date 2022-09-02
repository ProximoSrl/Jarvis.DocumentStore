using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Shared.Jobs;
using Jarvis.DocumentStore.Shared.Model;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.ProjectionTests;
using Jarvis.DocumentStore.Tests.Support;
using Jarvis.Framework.Kernel.MultitenantSupport;
using Jarvis.Framework.Kernel.ProjectionEngine;
using Jarvis.Framework.Shared.Helpers;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Shared.MultitenantSupport;
using Jarvis.Framework.Shared.ReadModel;
using Jarvis.NEventStoreEx.CommonDomainEx.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat = Jarvis.DocumentStore.Client.Model.DocumentFormat;
using DocumentHandle = Jarvis.DocumentStore.Client.Model.DocumentHandle;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;

// ReSharper disable InconsistentNaming
namespace Jarvis.DocumentStore.Tests.SelfHostIntegratonTests
{
    //[TestFixture("v1")]
    [TestFixture("v3")]
    [Category("Integration")]
    [Category("Slow")]
    public class DocumentControllerIntegrationTests
    {
        private DocumentStoreBootstrapper _documentStoreService;
        private DocumentStoreServiceClient _documentStoreClient;
        private IMongoCollection<DocumentDescriptorReadModel> _documentDescriptorCollection;
        private IMongoCollection<DocumentReadModel> _documentCollection;
        private IMongoCollection<BsonDocument> _commitCollection;

        private ITriggerProjectionsUpdate _projections;
        private ITenant _tenant;
        private IBlobStore _blobStore;
        private readonly String _engineVersion;

        public DocumentControllerIntegrationTests(String engineVersion)
        {
            _engineVersion = engineVersion;
        }

        private async Task UpdateAndWaitAsync()
        {
            await _projections.UpdateAndWait();
        }

        [SetUp]
        public void SetUp()
        {
            var config = new DocumentStoreTestConfiguration(_engineVersion);
            MongoDbTestConnectionProvider.DropTestsTenant();
            config.SetTestAddress(TestConfig.TestHostServiceAddress);
            _documentStoreService = new DocumentStoreBootstrapper();
            _documentStoreService.Start(config);
            _documentStoreClient = new DocumentStoreServiceClient(
                TestConfig.ServerAddress,
                TestConfig.Tenant
            );
            _tenant = ContainerAccessor.Instance.Resolve<TenantManager>().GetTenant(new TenantId(TestConfig.Tenant));

            //Issue: https://github.com/ProximoSrl/Jarvis.DocumentStore/issues/26
            //you need to resolve the IReader that in turns resolves the ProjectionEngine, becauase if you
            //directly resolve the ITriggerProjectionsUpdate, projection engine will be resolved multiple times.
            _tenant.Container.Resolve<IReader<StreamReadModel, Int64>>();
            _projections = _tenant.Container.Resolve<ITriggerProjectionsUpdate>();

            _documentDescriptorCollection = MongoDbTestConnectionProvider.ReadModelDb.GetCollection<DocumentDescriptorReadModel>("rm.DocumentDescriptor");
            _documentCollection = MongoDbTestConnectionProvider.ReadModelDb.GetCollection<DocumentReadModel>("rm.Document");
            _commitCollection = MongoDbTestConnectionProvider.ReadModelDb.GetCollection<BsonDocument>("Commits");
            _blobStore = _tenant.Container.Resolve<IBlobStore>();

            MongoFlatMapper.EnableFlatMapping(true);
        }

        [TearDown]
        public void TearDown()
        {
            _documentStoreService.Stop();
            BsonClassMapHelper.Clear();
        }

        [Test]
        public async Task Should_get_info_without_content()
        {
            var documentHandle = DocumentHandle.FromString("Pdf_2");

            await _documentStoreClient.UploadAsync(
                TestConfig.PathToDocumentPdf,
                documentHandle
            );

            // waits for storage
            await UpdateAndWaitAsync().ConfigureAwait(false);
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
        public async Task Should_download_with_range_header()
        {
            var documentHandle = DocumentHandle.FromString("Pdf_2");

            await _documentStoreClient.UploadAsync(
                TestConfig.PathToDocumentPdf,
                documentHandle
            );

            // waits for storage
            await UpdateAndWaitAsync().ConfigureAwait(false);
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
                Assert.AreEqual("bytes 0-199/72768", reader.ResponseData[HttpResponseHeader.ContentRange]);
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
                Assert.AreEqual("bytes 200-72767/72768", reader.ResponseData[HttpResponseHeader.ContentRange]);
            }
        }

        [Test]
        public async Task Should_upload_and_download_original_format()
        {
            await _documentStoreClient.UploadAsync(
                TestConfig.PathToDocumentPdf,
                DocumentHandle.FromString("Pdf_2"),
                new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            // waits for storage
            await UpdateAndWaitAsync().ConfigureAwait(false);

            var reader = _documentStoreClient.OpenRead(DocumentHandle.FromString("Pdf_2"));
            using (var downloaded = new MemoryStream())
            using (var uploaded = new MemoryStream())
            {
                using (var fileStream = File.OpenRead(TestConfig.PathToDocumentPdf))
                {
                    await fileStream.CopyToAsync(uploaded).ConfigureAwait(false);
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
        public async Task Should_upload_file_with_custom_data()
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
            await UpdateAndWaitAsync().ConfigureAwait(false);

            var customData = await _documentStoreClient.GetCustomDataAsync(DocumentHandle.FromString("Pdf_1"));
            Assert.NotNull(customData);
            Assert.IsTrue(customData.ContainsKey("callback"));
            Assert.AreEqual("http://localhost/demo", customData["callback"]);
        }

        [Test]
        public async Task Should_upload_with_a_stream()
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
        public async Task Should_get_document_formats()
        {
            var handle = new DocumentHandle("Formats");
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, handle);

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);
            var formats = await _documentStoreClient.GetFormatsAsync(handle);
            Assert.NotNull(formats);
            Assert.IsTrue(formats.HasFormat(new DocumentFormat("original")));
        }

        [Test]
        public async Task Can_add_new_format_with_api()
        {
            //Upload original
            var handle = new DocumentHandle("Add_Format_Test");
            await _documentStoreClient.UploadAsync(TestConfig.PathToOpenDocumentText, handle);

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

            //now add format to document.
            AddFormatFromFileToDocumentModel model = new AddFormatFromFileToDocumentModel();
            model.DocumentHandle = handle;
            model.PathToFile = TestConfig.PathToTextDocument;
            model.CreatedById = "tika";
            model.Format = new DocumentFormat("tika");
            await _documentStoreClient.AddFormatToDocument(model, new Dictionary<String, Object>());

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);
            var formats = await _documentStoreClient.GetFormatsAsync(handle);
            Assert.NotNull(formats);
            Assert.IsTrue(formats.HasFormat(new DocumentFormat("original")));
            Assert.IsTrue(formats.HasFormat(new DocumentFormat("tika")));
            Assert.That(formats, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task Can_add_new_format_with_api_and_automatic_format_detection()
        {
            //Upload original
            var handle = new DocumentHandle("Add_Format_Test");
            await _documentStoreClient.UploadAsync(TestConfig.PathToOpenDocumentText, handle);

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

            //now add format to document.
            AddFormatFromFileToDocumentModel model = new AddFormatFromFileToDocumentModel();
            model.DocumentHandle = handle;
            model.PathToFile = TestConfig.PathToDocumentPdf;
            model.CreatedById = "office";
            model.Format = null; //NO FORMAT, I want document store to be able to detect format
            await _documentStoreClient.AddFormatToDocument(model, new Dictionary<String, Object>());

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);
            var formats = await _documentStoreClient.GetFormatsAsync(handle);
            Assert.NotNull(formats);
            Assert.IsTrue(formats.HasFormat(new DocumentFormat("original")));
            Assert.IsTrue(formats.HasFormat(new DocumentFormat("pdf")));
            Assert.That(formats, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task Can_add_new_format_with_api_from_object()
        {
            //Upload original
            var handle = new DocumentHandle("Add_Format_Test");
            await _documentStoreClient.UploadAsync(TestConfig.PathToOpenDocumentText, handle);

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

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
            await UpdateAndWaitAsync().ConfigureAwait(false);
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
        public async Task Removing_format_from_document()
        {
            //Upload original
            var handle = new DocumentHandle("Add_Format_Test");
            await _documentStoreClient.UploadAsync(TestConfig.PathToOpenDocumentText, handle);

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

            //now add format to document.
            AddFormatFromFileToDocumentModel model = new AddFormatFromFileToDocumentModel();
            model.DocumentHandle = handle;
            model.PathToFile = TestConfig.PathToTextDocument;
            model.CreatedById = "tika";
            model.Format = new DocumentFormat("tika");
            await _documentStoreClient.AddFormatToDocument(model, new Dictionary<String, Object>());

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

            //now delete format
            await _documentStoreClient.RemoveFormatFromDocument(handle, new DocumentFormat("tika")).ConfigureAwait(false);

            await UpdateAndWaitAsync().ConfigureAwait(false);

            var formats = await _documentStoreClient.GetFormatsAsync(handle).ConfigureAwait(false);
            Assert.NotNull(formats);
            Assert.IsTrue(formats.HasFormat(new DocumentFormat("original")));
            Assert.That(formats, Has.Count.EqualTo(1), "Tika format should be removed from the projection");

            //Uncomment the test if you want to verify that blob id is deleted from a projection
            //Assert.Throws<Exception>(() => _blobStore.GetDescriptor(blobId), "Blob Id for artifact is not deleted");
        }

        [Test]
        public async Task Adding_two_time_same_format_overwrite_older()
        {
            //Upload original
            var handle = new DocumentHandle("Add_Format_Test");
            await _documentStoreClient.UploadAsync(TestConfig.PathToOpenDocumentText, handle);

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

            //now add format to document.
            AddFormatFromFileToDocumentModel model = new AddFormatFromFileToDocumentModel();
            model.DocumentHandle = handle;
            model.PathToFile = TestConfig.PathToTextDocument;
            model.CreatedById = "tika";
            model.Format = new DocumentFormat("tika");
            await _documentStoreClient.AddFormatToDocument(model, new Dictionary<String, Object>());

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

            //get blobId of the original format
            var descriptor = _documentDescriptorCollection.FindAll().Single();

            //now add same format with different content.
            model = new AddFormatFromFileToDocumentModel();
            model.DocumentHandle = handle;
            model.PathToFile = TestConfig.PathToHtml;
            model.CreatedById = "tika";
            model.Format = new DocumentFormat("tika");
            await _documentStoreClient.AddFormatToDocument(model, new Dictionary<String, Object>());

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);
            var formats = await _documentStoreClient.GetFormatsAsync(handle);
            Assert.NotNull(formats);
            Assert.IsTrue(formats.HasFormat(new DocumentFormat("original")));
            Assert.IsTrue(formats.HasFormat(new DocumentFormat("tika")));
            Assert.That(formats, Has.Count.EqualTo(2));

            await CompareDownloadedStreamToFile(TestConfig.PathToHtml, _documentStoreClient.OpenRead(handle, new DocumentFormat("tika"))).ConfigureAwait(false);

            //verify old blob storage was deleted
            //Assert.Throws<Exception>(() => _blobStore.GetDescriptor(blobId), "Blob Id for artifact is not deleted");
        }

        [Test]
        public async Task Can_add_attachment_to_existing_handle()
        {
            //Upload father
            var fatherHandle = new DocumentHandle("father");
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, fatherHandle).ConfigureAwait(false);

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

            await _documentStoreClient
                .UploadAttachmentAsync(TestConfig.PathToDocumentPng, fatherHandle, "Content", Path.GetFileName(TestConfig.PathToDocumentPng))
                .ConfigureAwait(false);

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

            var document = _documentDescriptorCollection.Find(Builders<DocumentDescriptorReadModel>.Filter.Eq("Documents", "content_1")).SingleOrDefault();
            Assert.That(document, Is.Not.Null, "Document with child handle was not find.");

            var handle = _documentDescriptorCollection.Find(Builders<DocumentDescriptorReadModel>.Filter.Eq("Documents", "father")).SingleOrDefault();
            Assert.That(handle, Is.Not.Null, "Father Handle Not Find");
            Assert.That(handle.Attachments.Select(a => a.Handle), Is.EquivalentTo(new[] {
                new Jarvis.DocumentStore.Core.Model.DocumentHandle("content_1")
            }));
        }

        [Test]
        public async Task Add_multiple_attachment_to_existing_handle()
        {
            //Upload father
            var fatherHandle = new DocumentHandle("father");
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, fatherHandle);
            await UpdateAndWaitAsync().ConfigureAwait(false);

            //upload attachments
            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToDocumentPng, fatherHandle, "Content", Path.GetFileName(TestConfig.PathToDocumentPng)).ConfigureAwait(false);
            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToOpenDocumentText, fatherHandle, "Content", Path.GetFileName(TestConfig.PathToOpenDocumentText)).ConfigureAwait(false);
            await UpdateAndWaitAsync().ConfigureAwait(false);

            var document = _documentDescriptorCollection.Find(Builders<DocumentDescriptorReadModel>.Filter.Eq("Documents", "content_1")).SingleOrDefault();
            Assert.That(document, Is.Not.Null, "Document with first child handle was not find.");

            document = _documentDescriptorCollection.Find(Builders<DocumentDescriptorReadModel>.Filter.Eq("Documents", "content_2")).SingleOrDefault();
            Assert.That(document, Is.Not.Null, "Document with second child handle was not find.");

            var handle = _documentDescriptorCollection.Find(Builders<DocumentDescriptorReadModel>.Filter.Eq("Documents", "father")).SingleOrDefault();
            Assert.That(handle, Is.Not.Null, "Father Handle Not Find");
            Assert.That(handle.Attachments.Select(a => a.Handle), Is.EquivalentTo(new[] { new Core.Model.DocumentHandle("content_1"), new Core.Model.DocumentHandle("content_2") }));
        }

        [Test]
        public async Task Add_multiple_time_same_handle_with_same_payload()
        {
            //Upload father
            var theHandle = new DocumentHandle("a_pdf_file");
            List<Task> jobs = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                var task = _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, theHandle);
                jobs.Add(task);
            }
            Thread.Sleep(1000);
            foreach (var job in jobs)
            {
                await job;
            }

            await UpdateAndWaitAsync().ConfigureAwait(false);

            var documents = _documentDescriptorCollection.FindAll();

            Assert.That(documents.Count(), Is.EqualTo(1), "We expect all document to be de-duplicated.");

            var document = documents.Single();
            Assert.That(document.Created, Is.True, "Document descriptor should be in created-state.");
        }

        [Test]
        public async Task add_multiple_attachment_to_existing_handle_then_delete_handle()
        {
            //Upload father
            var fatherHandle = new DocumentHandle("father");
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, fatherHandle);
            await UpdateAndWaitAsync().ConfigureAwait(false);

            //upload attachments
            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToDocumentPng, fatherHandle, "Zip", Path.GetFileName(TestConfig.PathToDocumentPng));
            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToOpenDocumentText, fatherHandle, "Zip", Path.GetFileName(TestConfig.PathToOpenDocumentText));
            await UpdateAndWaitAsync().ConfigureAwait(false);

            await _documentStoreClient.DeleteAsync(fatherHandle);
            await UpdateAndWaitAsync().ConfigureAwait(false);

            Assert.That(_documentDescriptorCollection.AsQueryable().Count(), Is.EqualTo(0), "Attachment should be deleted.");
            Assert.That(_documentCollection.AsQueryable().Count(), Is.EqualTo(0), "Attachment should be deleted.");
        }

        [Test]
        public async Task can_retrieve_attachments()
        {
            //Upload father
            var fatherHandle = new DocumentHandle("father");
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, fatherHandle);

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToDocumentPng, fatherHandle, "source", Path.GetFileName(TestConfig.PathToDocumentPng));

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

            var attachments = await _documentStoreClient.GetAttachmentsAsync(fatherHandle);
            Assert.NotNull(attachments);
            Assert.That(attachments.Attachments.Length, Is.EqualTo(1));
            Assert.That(attachments.Attachments[0].RelativePath.ToString(), Is.EqualTo(Path.GetFileName(TestConfig.PathToDocumentPng)));
            Assert.That(attachments.Attachments[0].Handle.ToString(), Is.EqualTo("http://localhost:5123/tests/documents/source_1"));
        }

        [Test]
        public async Task verify_de_duplication_delete_original_blob()
        {
            DateTime now = DateTime.UtcNow.AddDays(+30);

            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, new DocumentHandle("handleA"));
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, new DocumentHandle("handleB"));
            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);
            using (DateTimeService.Override(() => now))
            {
                //now we need to wait cleanupJobs to start 
                ExecuteCleanupJob();
            }
            //verify that blob
            Assert.That(_blobStore.GetDescriptor(new BlobId("original.1")), Is.Not.Null);

            Assert.Throws<Exception>(() => _blobStore.GetDescriptor(new BlobId("original.2")));

        }

        [Test]
        public async Task verify_de_duplication__not_delete_original_blob_before_15_days()
        {
            DateTime now = DateTime.UtcNow.AddDays(+14);

            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, new DocumentHandle("handleA"));
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, new DocumentHandle("handleB"));
            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);
            using (DateTimeService.Override(() => now))
            {
                //now we need to wait cleanupJobs to start 
                ExecuteCleanupJob();
            }
            //verify that blob
            Assert.That(_blobStore.GetDescriptor(new BlobId("original.1")), Is.Not.Null);
            Assert.That(_blobStore.GetDescriptor(new BlobId("original.2")), Is.Not.Null);
        }

        [Test]
        public async Task attachments_not_retrieve_nested_attachment()
        {
            //Upload father
            var fatherHandle = new DocumentHandle("father");
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, fatherHandle);

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToDocumentPng, fatherHandle, "source", Path.GetFileName(TestConfig.PathToDocumentPng));

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToDocumentPng, new DocumentHandle("source_1"), "nested", Path.GetFileName(TestConfig.PathToDocumentPng));

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

            var attachments = await _documentStoreClient.GetAttachmentsAsync(fatherHandle);
            Assert.NotNull(attachments);
            Assert.That(attachments.Attachments.Length, Is.EqualTo(1));
            Assert.That(attachments.Attachments[0].RelativePath, Is.EqualTo(Path.GetFileName(TestConfig.PathToDocumentPng)));
            Assert.That(attachments.Attachments[0].Handle, Is.EqualTo("http://localhost:5123/tests/documents/source_1"));
        }

        [Test]
        public async Task attachments_fat_retrieve_nested_attachment()
        {
            //Upload father
            var fatherHandle = new DocumentHandle("father");
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, fatherHandle);

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToDocumentPng, fatherHandle, "source", Path.GetFileName(TestConfig.PathToDocumentPng));

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

            await _documentStoreClient.UploadAttachmentAsync(TestConfig.PathToExcelDocument, new DocumentHandle("source_1"), "nested", Path.GetFileName(TestConfig.PathToExcelDocument));

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

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
                    await fileStream.CopyToAsync(uploaded).ConfigureAwait(false);
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
        public async Task should_upload_get_metadata_and_delete_a_document()
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
            await UpdateAndWaitAsync().ConfigureAwait(false);

            var data = await _documentStoreClient.GetCustomDataAsync(handle);

            await _documentStoreClient.DeleteAsync(handle);

            await UpdateAndWaitAsync().ConfigureAwait(false);

            var ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                await _documentStoreClient.GetCustomDataAsync(handle).ConfigureAwait(false);
            });

            Assert.IsTrue(ex.Message.Contains("404"));

            // check readmodel
            var tenantAccessor = ContainerAccessor.Instance.Resolve<ITenantAccessor>();
            var tenant = tenantAccessor.GetTenant(new TenantId(TestConfig.Tenant));
            var docReader = tenant.Container.Resolve<IMongoDbReader<DocumentDescriptorReadModel, DocumentDescriptorId>>();

            var allDocuments = docReader.AllUnsorted.Count();
            Assert.AreEqual(0, allDocuments);
        }

        [Test]
        public async Task upload_copy_handle_then_delete_original_handle()
        {
            var handle = DocumentHandle.FromString("PdfHandleToCopy");
            var copiedHandle = DocumentHandle.FromString("PdfHandleCopied");

            await _documentStoreClient.UploadAsync(
                TestConfig.PathToDocumentPdf,
                handle,
                new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

            await _documentStoreClient.CopyHandleAsync(handle, copiedHandle);

            await UpdateAndWaitAsync().ConfigureAwait(false);

            await _documentStoreClient.DeleteAsync(handle);

            await UpdateAndWaitAsync().ConfigureAwait(false);

            // check readmodel
            var tenantAccessor = ContainerAccessor.Instance.Resolve<ITenantAccessor>();
            var tenant = tenantAccessor.GetTenant(new TenantId(TestConfig.Tenant));
            var docReader = tenant.Container.Resolve<IMongoDbReader<DocumentDescriptorReadModel, DocumentDescriptorId>>();

            var allDocuments = docReader.AllUnsorted;
            Assert.AreEqual(1, allDocuments.Count());
            var singleDoc = docReader.AllUnsorted.Single();
            Assert.That(singleDoc.Documents.Select(h => h.ToString()), Is.EquivalentTo(new[] { copiedHandle.ToString() }));
        }

        [Test]
        public async Task can_upload_document_with_name_greater_than_250_char()
        {
            var handle = DocumentHandle.FromString("Pdf_3");
            String longFileName = Path.Combine(
                Path.GetTempPath(),
                "_lfn" + new string('X', 240) + ".pdf");
            if (!File.Exists(longFileName))
            {
                File.Copy(TestConfig.PathToDocumentPdf, longFileName);
            }

            await _documentStoreClient.UploadAsync(longFileName, handle);

            // wait background projection polling
            await UpdateAndWaitAsync().ConfigureAwait(false);

            // check readmodel
            var tenantAccessor = ContainerAccessor.Instance.Resolve<ITenantAccessor>();
            var tenant = tenantAccessor.GetTenant(new TenantId(TestConfig.Tenant));
            var docReader = tenant.Container.Resolve<IMongoDbReader<DocumentDescriptorReadModel, DocumentDescriptorId>>();

            var allDocuments = docReader.AllUnsorted.Count();
            Assert.AreEqual(1, allDocuments);
        }

        [Test]
        public async Task verify_de_duplication_not_link_to_deleted_handles()
        {
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, new DocumentHandle("handleA"));
            await UpdateAndWaitAsync().ConfigureAwait(false);

            await _documentStoreClient.DeleteAsync(new DocumentHandle("handleA"));
            await UpdateAndWaitAsync().ConfigureAwait(false);

            //re-add same payload with same handle
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, new DocumentHandle("handleB"));
            await UpdateAndWaitAsync().ConfigureAwait(false);

            //verify that everything is ok.
            var allDescriptor = _documentDescriptorCollection.FindAll().ToList();
            Assert.That(allDescriptor, Has.Count.EqualTo(1));
            Assert.That(allDescriptor[0].Documents, Is.EquivalentTo(new[] { new Core.Model.DocumentHandle("handleB") }));
        }

        [Test]
        public async Task verify_de_duplication_not_link_to_deleted_handles_same_handle()
        {
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, new DocumentHandle("handleA"));
            await UpdateAndWaitAsync().ConfigureAwait(false);

            await _documentStoreClient.DeleteAsync(new DocumentHandle("handleA"));
            await UpdateAndWaitAsync().ConfigureAwait(false);

            //re-add same payload with same handle
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, new DocumentHandle("handleA"));
            await UpdateAndWaitAsync().ConfigureAwait(false);

            //verify that everything is ok.
            var allDescriptor = _documentDescriptorCollection.FindAll().ToList();
            Assert.That(allDescriptor, Has.Count.EqualTo(1));
            Assert.That(allDescriptor[0].Documents, Is.EquivalentTo(new[] { new Core.Model.DocumentHandle("handleA") }));
        }

        [Test]
        public async Task verify_delete_then_re_add_handle()
        {
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, new DocumentHandle("handleA"));
            await UpdateAndWaitAsync().ConfigureAwait(false);

            await _documentStoreClient.DeleteAsync(new DocumentHandle("handleA"));
            await UpdateAndWaitAsync().ConfigureAwait(false);

            //re-add same payload with same handle
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPng, new DocumentHandle("handleA"));
            await UpdateAndWaitAsync().ConfigureAwait(false);

            //verify that everything is ok.
            var allDescriptor = _documentDescriptorCollection.FindAll().ToList();
            Assert.That(allDescriptor, Has.Count.EqualTo(1));
            Assert.That(allDescriptor[0].Documents, Is.EquivalentTo(new[] { new Core.Model.DocumentHandle("handleA") }));
        }

        [Test]
        public async Task verify_delete_remove_document_with_cleanup()
        {
            DateTime now = DateTime.UtcNow.AddDays(+30);
            var repo = _tenant.Container.Resolve<IRepositoryEx>();
            await _documentStoreClient.UploadAsync(TestConfig.PathToDocumentPdf, new DocumentHandle("handleX"));
            await UpdateAndWaitAsync().ConfigureAwait(false);

            await _documentStoreClient.DeleteAsync(new DocumentHandle("handleX"));
            await UpdateAndWaitAsync().ConfigureAwait(false);

            using (DateTimeService.Override(() => now))
            {
                //now we need to wait cleanupJobs to start 
                ExecuteCleanupJob();
            }

            var aggregate = repo.GetById<Document>(new DocumentId(1L));
            Assert.That(aggregate.Version, Is.EqualTo(0));
        }

        #region Helpers

        private void ExecuteCleanupJob()
        {
            CleanupJob job = _tenant.Container.Resolve<CleanupJob>();
            IJobExecutionContext context = NSubstitute.Substitute.For<IJobExecutionContext>();
            IJobDetail jobDetail = NSubstitute.Substitute.For<IJobDetail>();
            IDictionary<string, object> mapd = new Dictionary<string, object>() { { JobKeys.TenantId.ToString(), _tenant.Id.ToString() } };
            JobDataMap map = new JobDataMap(mapd);
            jobDetail.JobDataMap.Returns(map);
            context.JobDetail.Returns(jobDetail);
            job.Execute(context);
        }

        #endregion
    }
}
