using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Logging;
using com.sun.tools.javah;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Jobs.Email;
using Jarvis.DocumentStore.Jobs.HtmlZipOld;
using Jarvis.DocumentStore.Jobs.ImageResizer;
using Jarvis.DocumentStore.Jobs.Tika;
using Jarvis.DocumentStore.JobsHost.Helpers;
using Jarvis.DocumentStore.JobsHost.Support;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using Jarvis.Framework.Kernel.MultitenantSupport;
using Jarvis.Framework.Kernel.ProjectionEngine;
using Jarvis.Framework.Shared.MultitenantSupport;
using Jarvis.Framework.TestHelpers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NUnit.Framework;
using Jarvis.DocumentStore.Core.Storage;
using System.Drawing;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Jarvis.DocumentStore.Tests.ProjectionTests;
using Newtonsoft.Json;
using System.Threading;
using ContainerAccessor = Jarvis.DocumentStore.Host.Support.ContainerAccessor;
using Jarvis.DocumentStore.Jobs.Attachments;
using Jarvis.DocumentStore.Core;
using DocumentFormat = Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.DocumentFormat;
using MongoDB.Bson;
using Jarvis.DocumentStore.Jobs.LibreOffice;

// ReSharper disable InconsistentNaming
namespace Jarvis.DocumentStore.Tests.SelfHostIntegratonTests
{
    /// <summary>
    /// Test of jobs executed out of process. This tests execute a full 
    /// 1) reset
    /// 2) upload document and wait for specific queue to process the docs
    /// 3) verify the outcome.
    /// </summary>
    [TestFixture]
    [Category("integration_full")]
    //[Explicit("This integration test is slow because it wait for polling")]
    public abstract class DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public const int MaxTimeout = 10000;

        protected DocumentStoreBootstrapper _documentStoreService;
        protected DocumentStoreServiceClient _documentStoreClient;
        protected MongoCollection<DocumentDescriptorReadModel> _documentDescriptorCollection;
        protected MongoCollection<DocumentReadModel> _documentCollection;
        protected DocumentStoreTestConfigurationForPollQueue _config;
        protected JobsHostConfiguration _jobsHostConfiguration;
        protected IBlobStore _blobStore;
        protected AbstractOutOfProcessPollerJob _sutBase;

        private ITriggerProjectionsUpdate _projections;

        protected void UpdateAndWait()
        {
            _projections.UpdateAndWait();
            Thread.Sleep(1000); //update and wait returns immediately if there are no other stuff to dispatch.
        }

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _config = new DocumentStoreTestConfigurationForPollQueue(OnGetQueueInfo());
            _jobsHostConfiguration = new JobsHostConfiguration();
            MongoDbTestConnectionProvider.DropTenant(TestConfig.Tenant);
            _config.ServerAddress = TestConfig.ServerAddress;
            _documentStoreService = new DocumentStoreBootstrapper();
            _documentStoreService.Start(_config);
            _documentStoreClient = new DocumentStoreServiceClient(
                TestConfig.ServerAddress,
                TestConfig.Tenant
            );
            _documentDescriptorCollection = MongoDbTestConnectionProvider.ReadModelDb.GetCollection<DocumentDescriptorReadModel>("rm.DocumentDescriptor");
            _documentCollection = MongoDbTestConnectionProvider.ReadModelDb.GetCollection<DocumentReadModel>("rm.Document");
            TenantContext.Enter(new TenantId(TestConfig.Tenant));
            var tenant = ContainerAccessor.Instance.Resolve<TenantManager>().Current;
            _projections = tenant.Container.Resolve<ITriggerProjectionsUpdate>();
            _blobStore = tenant.Container.Resolve<IBlobStore>();
        }

        protected abstract QueueInfo[] OnGetQueueInfo();

        protected void PrepareJob(String handle = "TESTHANDLE", AbstractOutOfProcessPollerJob testJob = null)
        {
            var job = testJob ?? _sutBase;
            job.JobsHostConfiguration = _jobsHostConfiguration;
            job.Logger = new TestLogger(LoggerLevel.Error);
            job.Start(new List<string>() { TestConfig.ServerAddress.AbsoluteUri }, handle);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            _documentStoreService.Stop();
            BsonClassMapHelper.Clear();
            OnStop();
        }

        protected virtual void OnStop()
        {
            if (_sutBase != null) _sutBase.Stop();
        }
    }

    [TestFixture]
    [Category("integration_full")]
    public class integration_out_of_process_tika : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        OutOfProcessTikaNetJob sut;

        [Test]
        public async void verify_tika_job()
        {
            _sutBase = sut = new OutOfProcessTikaNetJob();
            PrepareJob();

            var handleCore = new Jarvis.DocumentStore.Core.Model.DocumentHandle("verify_tika_job");
            var handleClient = new DocumentHandle("verify_tika_job");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToWordDocument,
               handleClient,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            DateTime startWait = DateTime.Now;
            DocumentDescriptorReadModel documentDescriptor;
            var format = new DocumentFormat("tika");
            do
            {
                UpdateAndWait();
                documentDescriptor = _documentDescriptorCollection.AsQueryable()
                    .SingleOrDefault(d => d.Documents.Contains(handleCore));
                if (documentDescriptor != null &&
                    documentDescriptor.Formats.ContainsKey(format))
                {
                    //Document found, but we want to be sure that the fileName of the format is correct.
                    var formatInfo = documentDescriptor.Formats[format];
                    var blob = _blobStore.GetDescriptor(formatInfo.BlobId);
                    Assert.That(
                        blob.FileNameWithExtension.ToString(),
                        Is.EqualTo(Path.GetFileNameWithoutExtension(TestConfig.PathToWordDocument) + ".tika.html"),
                        "File name is wrong, we expect the same file name with extension .tika.html");
                    return;
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            Assert.Fail("Tika document not found");
        }


        protected override QueueInfo[] OnGetQueueInfo()
        {
            return new QueueInfo[]
            {
                new QueueInfo("tika", "original", ""), 
            };
        }
    }

    [TestFixture]
    [Category("integration_full")]
    public class integration_out_of_process_tika_content : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        OutOfProcessTikaNetJob sut;

        [Test]
        public async void verify_tika_set_content()
        {
            _sutBase = sut = new OutOfProcessTikaNetJob();
            PrepareJob();

            var handle = DocumentHandle.FromString("verify_tika_job");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToWordDocument,
               handle,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            DateTime startWait = DateTime.Now;
            DocumentDescriptorReadModel documentDescriptor;
            var format = new DocumentFormat("content");
            do
            {
                UpdateAndWait();
                documentDescriptor = _documentDescriptorCollection.AsQueryable()
                    .SingleOrDefault(d => d.Documents.Contains(new Core.Model.DocumentHandle("verify_tika_job")));
                if (documentDescriptor != null &&
                    documentDescriptor.Formats.ContainsKey(format))
                {
                    //now we want to verify if content is set correctly
                    var client = new DocumentStoreServiceClient(TestConfig.ServerAddress, TestConfig.Tenant);
                    var content = await client.GetContentAsync(handle);
                    Assert.That(content.Pages.Length, Is.EqualTo(1));
                    Assert.That(content.Pages[0].Content, Contains.Substring("word document").IgnoreCase);

                    var formatInfo = documentDescriptor.Formats[format];
                    var blob = _blobStore.GetDescriptor(formatInfo.BlobId);
                    Assert.That(
                        blob.FileNameWithExtension.ToString(),
                        Is.EqualTo(Path.GetFileNameWithoutExtension(TestConfig.PathToWordDocument) + ".content"),
                        "File name is wrong, we expect the same file name with extension .content");

                    return;
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            Assert.Fail("Tika document not found");
        }

        protected override QueueInfo[] OnGetQueueInfo()
        {
            return new QueueInfo[]
            {
                new QueueInfo("tika", "original", ""), 
            };
        }
    }

    [TestFixture]
    [Category("integration_full")]
    public class integration_out_of_process_image : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        [Test]
        public async void verify_image_resizer_job()
        {
            ImageResizePollerOutOfProcessJob sut = new ImageResizePollerOutOfProcessJob();
            sut.JobsHostConfiguration = _jobsHostConfiguration;
            sut.Logger = new TestLogger(LoggerLevel.Error);
            sut.Start(new List<string>() { TestConfig.ServerAddress.AbsoluteUri }, "TESTHANDLE");
            DocumentHandle handle = DocumentHandle.FromString("verify_img_resize_job");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToDocumentPng,
               handle,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            DateTime startWait = DateTime.Now;
            DocumentDescriptorReadModel documentDescriptor;
            var thumbSmallFormat = new DocumentFormat("thumb.small");
            var thumbLargeFormat = new DocumentFormat("thumb.large");
            do
            {
                UpdateAndWait();
                documentDescriptor = _documentDescriptorCollection.AsQueryable()
                    .SingleOrDefault(d => d.Documents.Contains(new Core.Model.DocumentHandle("verify_img_resize_job")));
                if (documentDescriptor != null &&
                    documentDescriptor.Formats.ContainsKey(thumbSmallFormat) &&
                    documentDescriptor.Formats.ContainsKey(thumbLargeFormat))
                {
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(TestConfig.PathToDocumentPng);
                    var blobId = documentDescriptor.Formats[thumbSmallFormat].BlobId;
                    var file = _blobStore.GetDescriptor(blobId);
                    Assert.That(file.FileNameWithExtension.ToString(), Is.EqualTo(fileNameWithoutExtension + ".small.png"));
                    var downloadedImage = _blobStore.Download(blobId, Path.GetTempPath());
                    using (var image = Image.FromFile(downloadedImage))
                    {
                        Assert.That(image.Height, Is.EqualTo(200));
                    }

                    blobId = documentDescriptor.Formats[thumbLargeFormat].BlobId;
                    file = _blobStore.GetDescriptor(blobId);
                    Assert.That(file.FileNameWithExtension.ToString(), Is.EqualTo(fileNameWithoutExtension + ".large.png"));
                    downloadedImage = _blobStore.Download(blobId, Path.GetTempPath());
                    using (var image = Image.FromFile(downloadedImage))
                    {
                        Assert.That(image.Height, Is.EqualTo(800));
                    }
                    return; //test is good
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            Assert.Fail("Thumb small not found");
        }

        protected override QueueInfo[] OnGetQueueInfo()
        {
            var imgResizeQueue = new QueueInfo("imgResize", "^(?!img$).*", "png|jpg|gif|jpeg");
            imgResizeQueue.Parameters = new System.Collections.Generic.Dictionary<string, string>();
            imgResizeQueue.Parameters.Add("thumb_format", "png");
            imgResizeQueue.Parameters.Add("sizes", "small:200x200|large:800x800");
            return new QueueInfo[]
            {
                imgResizeQueue,
            };
        }
    }

    [TestFixture]
    [Category("integration_full")]
    public class integration_out_of_process_eml : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        AnalyzeEmailOutOfProcessJob sut;

        [Test]
        public async void verify_chain_for_email()
        {
            _sutBase = sut = new AnalyzeEmailOutOfProcessJob();
            PrepareJob();

            DocumentHandle handle = DocumentHandle.FromString("verify_chain_for_email");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToEml,
               handle,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            DateTime startWait = DateTime.Now;
            DocumentDescriptorReadModel documentDescriptor;
            var emailFormat = new DocumentFormat("email");
            do
            {
                UpdateAndWait();
                documentDescriptor = _documentDescriptorCollection.AsQueryable()
                    .SingleOrDefault(d => d.Documents.Contains(new Core.Model.DocumentHandle("verify_chain_for_email")));
                if (documentDescriptor != null &&
                    documentDescriptor.Formats.ContainsKey(emailFormat))
                {

                    return; //test is good
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            Assert.Fail("expected formats not found");
        }

        HtmlToPdfOutOfProcessJobOld _htmlSut;

        protected override void OnStop()
        {
            base.OnStop();
            if (_htmlSut != null) _htmlSut.Stop();
        }
        protected override QueueInfo[] OnGetQueueInfo()
        {
            return new QueueInfo[]
            {
                new QueueInfo("email", "", "eml|msg"),
            };
        }
    }

    [TestFixture]
    [Category("integration_full")]
    public class integration_out_of_process_eml_chain : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        AnalyzeEmailOutOfProcessJob sut;

        HtmlToPdfOutOfProcessJobOld _htmlSut;

        [Test]
        public async void verify_full_chain_for_email_and_html_zipOld()
        {
            _sutBase = sut = new AnalyzeEmailOutOfProcessJob();
            PrepareJob();

            HtmlToPdfOutOfProcessJobOld htmlSut = new HtmlToPdfOutOfProcessJobOld();
            PrepareJob(testJob: htmlSut);

            DocumentHandle handle = DocumentHandle.FromString("verify_chain_for_email");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToEml,
               handle,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            DateTime startWait = DateTime.Now;
            DocumentDescriptorReadModel documentDescriptor;
            var emailFormat = new DocumentFormat("email");
            var pdfFormat = new DocumentFormat("Pdf");
            do
            {
                UpdateAndWait();
                documentDescriptor = _documentDescriptorCollection.AsQueryable()
                    .SingleOrDefault(d => d.Documents.Contains(new Core.Model.DocumentHandle("verify_chain_for_email")));
                if (documentDescriptor != null &&
                    documentDescriptor.Formats.ContainsKey(emailFormat) &&
                    documentDescriptor.Formats.ContainsKey(pdfFormat))
                {

                    return; //test is good
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            if (documentDescriptor == null)
                Assert.Fail("Missing document");

            Debug.WriteLine("document:\n{0}", (object)JsonConvert.SerializeObject(documentDescriptor, Formatting.Indented));

            if (!documentDescriptor.Formats.ContainsKey(emailFormat))
                Assert.Fail("Missing format: {0}", emailFormat);

            if (!documentDescriptor.Formats.ContainsKey(pdfFormat))
                Assert.Fail("Missing format: {0}", pdfFormat);
        }

        protected override void OnStop()
        {
            base.OnStop();
            if (_htmlSut != null) _htmlSut.Stop();
        }
        protected override QueueInfo[] OnGetQueueInfo()
        {
            return new QueueInfo[]
            {
                new QueueInfo("htmlzip", "", "htmlzip|ezip"),
                new QueueInfo("email", "", "eml|msg"),
            };
        }
    }

    [TestFixture]
    [Category("integration_full")]
    public class integration_out_of_process_html_to_pdf : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {

        public async Task<Boolean> verify_htmlToPdf_base<T>() where T : AbstractOutOfProcessPollerJob, new()
        {
            _sutBase = new T();
            PrepareJob();

            DocumentHandle handle = DocumentHandle.FromString("verify_chain_for_htmlzip");
            var client = new DocumentStoreServiceClient(TestConfig.ServerAddress, TestConfig.Tenant);
            var zipped = client.ZipHtmlPage(TestConfig.PathToHtml);

            await _documentStoreClient.UploadAsync(
               zipped,
               handle,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            DateTime startWait = DateTime.Now;
            DocumentDescriptorReadModel documentDescriptor;
            var pdfFormat = DocumentFormats.Pdf;
            do
            {
                UpdateAndWait();
                documentDescriptor = _documentDescriptorCollection.AsQueryable()
                    .SingleOrDefault(d => d.Documents.Contains(new Core.Model.DocumentHandle("verify_chain_for_htmlzip")));
                if (documentDescriptor != null &&
                    documentDescriptor.Formats.ContainsKey(pdfFormat))
                {

                    return true; //test is good
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            return false;
        }

        //[Test]
        //[Explicit("new version of TuesPechkin seems to hang the tests")]
        //public async void verify_htmlToPdf()
        //{
        //    Assert.That(await verify_htmlToPdf_base<HtmlToPdfOutOfProcessJob>(), "Format Pdf not found");
        //}

        [Test]
        public async void verify_htmlToPdf_old()
        {
            Assert.That(await verify_htmlToPdf_base<HtmlToPdfOutOfProcessJobOld>(), "Format Pdf not found");
        }

        protected override QueueInfo[] OnGetQueueInfo()
        {
            return new QueueInfo[]
            {
                new QueueInfo("htmlzip", "", "htmlzip|ezip"),
            };
        }
    }

    [TestFixture]
    [Category("integration_full")]
    public class integration_out_of_process_office_to_pdf : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {

        [Test]
        public async void verify_office_job()
        {
            JobsHostConfiguration config = new JobsHostConfiguration();
            var conversion = new LibreOfficeUnoConversion(config);
            _sutBase = new LibreOfficeToPdfOutOfProcessJob(conversion);
            PrepareJob();

            var handleClient = DocumentHandle.FromString("verify_office_job");
            var handleServer = new Jarvis.DocumentStore.Core.Model.DocumentHandle("verify_office_job");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToWordDocument,
               handleClient,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            DateTime startWait = DateTime.Now;
            DocumentDescriptorReadModel documentDescriptor;
            var format = new DocumentFormat("Pdf");
            do
            {
                UpdateAndWait();
                documentDescriptor = _documentDescriptorCollection.AsQueryable()
                    .SingleOrDefault(d => d.Documents.Contains(handleServer));
                if (documentDescriptor != null &&
                    documentDescriptor.Formats.ContainsKey(format))
                {
                    //Document found, but we want to be sure that the fileName of the format is correct.
                    var formatInfo = documentDescriptor.Formats[format];
                    var blob = _blobStore.GetDescriptor(formatInfo.BlobId);
                    Assert.That(
                        blob.FileNameWithExtension.ToString(),
                        Is.EqualTo(Path.GetFileNameWithoutExtension(TestConfig.PathToWordDocument) + ".pdf"),
                        "File name is wrong, we expect the same file name with extension .pdf");
                    return;
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            Assert.Fail("Pdf format not found");
        }


        protected override QueueInfo[] OnGetQueueInfo()
        {
            return new QueueInfo[]
            {
                new QueueInfo("office", "", "doc|docx"),
            };
        }
    }

    [TestFixture]
    [Category("integration_full")]
    public class integration_attachments_queue_multiple_zip : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {



        [Test]
        public async void verify_nested_zip_count_file_verification()
        {

            _sutBase = new AttachmentOutOfProcessJob();
            PrepareJob();

            var handleClient = DocumentHandle.FromString("verify_nested_zip");
            var handleServer = new Jarvis.DocumentStore.Core.Model.DocumentHandle("verify_zip");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToZipFileThatContainsOtherZip,
               handleClient,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            DateTime startWait = DateTime.Now;
            DocumentDescriptorReadModel documentDescriptor;
            var format = new DocumentFormat("Pdf");
            var docCount = 0;
            do
            {
                UpdateAndWait();
                docCount = _documentCollection.AsQueryable().Count();
                if (docCount >= 7)
                {
                    //all attachment are unzipped correctly

                    return;
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            Assert.Fail("documents not unzipped correctly I'm expecting more than 7 documetns but we find " + docCount);
        }


        protected override QueueInfo[] OnGetQueueInfo()
        {
            return new QueueInfo[]
            {
                new QueueInfo("attachments", mimeTypes : MimeTypes.GetMimeTypeByExtension("zip")),
            };
        }
    }

    [TestFixture]
    [Category("integration_full")]
    public class integration_attachments_queue_singleZip : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {

        [Test]
        public async void verify_single_zip_count_file_verification()
        {

            _sutBase = new AttachmentOutOfProcessJob();
            PrepareJob();

            var handleClient = DocumentHandle.FromString("verify_zip");
            var handleServer = new Jarvis.DocumentStore.Core.Model.DocumentHandle("verify_zip");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToZipFile,
               handleClient,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            DateTime startWait = DateTime.Now;
            DocumentDescriptorReadModel documentDescriptor;
            var format = new DocumentFormat("Pdf");
            Int32 docCount;
            do
            {
                UpdateAndWait();
                docCount = _documentCollection.AsQueryable().Count();
                if (docCount == 4)
                {
                    var doc = _documentCollection.FindOneById(BsonValue.Create(handleClient.ToString()));
                    Assert.That(doc.Attachments, Is.EquivalentTo(new[] {
                        new Core.Model.DocumentHandle("attachment_zip_1"),
                        new Core.Model.DocumentHandle("attachment_zip_2"),
                        new Core.Model.DocumentHandle("attachment_zip_3")
                    }));
                    return;
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            Assert.Fail("documents not unzipped correctly, I'm expecting 4 documetns but projection contains " + docCount + " documents");
        }



        protected override QueueInfo[] OnGetQueueInfo()
        {
            return new QueueInfo[]
            {
                new QueueInfo("attachments", mimeTypes : MimeTypes.GetMimeTypeByExtension("zip")),
            };
        }
    }


    [TestFixture]
    [Category("integration_full")]
    public class integration_attachment_then_same_attach_inside_externa_attach : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {

        [Test]
        public async void verify_attachment_chains()
        {

            _sutBase = new AttachmentOutOfProcessJob();
            PrepareJob();

            var handleClient = DocumentHandle.FromString("child_zip");
            var handleServer = new Jarvis.DocumentStore.Core.Model.DocumentHandle("child_zip");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToZipFile,
               handleClient,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            DateTime startWait = DateTime.Now;
            Int32 docCount;
            do
            {
                UpdateAndWait();
                docCount = _documentDescriptorCollection.AsQueryable().Count();
                if (docCount == 4)
                {
                    break;
                }
            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            //now all attachment are unzipped.
             handleClient = DocumentHandle.FromString("containing_zip");
             handleServer = new Jarvis.DocumentStore.Core.Model.DocumentHandle("containing_zip");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToZipFileThatContainsOtherZip,
               handleClient,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

             startWait = DateTime.Now;
            do
            {
                UpdateAndWait();
                docCount = _documentCollection.AsQueryable().Count();
                if (docCount == 8)
                {
                    var doc = _documentCollection.FindAll();
                    //now I'm expeting to find three documents with attachments
                    Assert.That(doc.Count(d => d.Attachments != null && d.Attachments.Count > 0), Is.EqualTo(3));

                    return;
                }
            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            Assert.Fail("documents not unzipped correctly, I'm expecting correct chain of doucments");
        }



        protected override QueueInfo[] OnGetQueueInfo()
        {
            return new QueueInfo[]
            {
                new QueueInfo("attachments", mimeTypes : MimeTypes.GetMimeTypeByExtension("zip")),
            };
        }
    }
    [TestFixture]
    [Category("integration_full")]
    public class integration_attachments_mail_with_attach : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {

        [Test]
        public async void verify_email_count_file_verification()
        {

            _sutBase = new AttachmentOutOfProcessJob();
            PrepareJob();

            var handleClient = DocumentHandle.FromString("verify_eml");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToMsgWithAttachment,
               handleClient,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            DateTime startWait = DateTime.Now;
            Int32 docCount;
            do
            {
                UpdateAndWait();
                docCount = _documentDescriptorCollection.AsQueryable().Count();
                if (docCount == 2)
                {
                    //all attachment are unzipped correctly
                    var doc = _documentCollection.FindOneById(BsonValue.Create(handleClient.ToString()));
                    Assert.That(doc.Attachments, Is.EquivalentTo(new[] { new Core.Model.DocumentHandle("attachment_email_1") }));
                    return;
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            Assert.Fail("file from email not extracted correctly, I'm expecting 2 documetns but projection contains " + docCount + " documents");
        }



        protected override QueueInfo[] OnGetQueueInfo()
        {

            return new QueueInfo[]
            {
                new QueueInfo("attachments", mimeTypes : 
                    MimeTypes.GetMimeTypeByExtension("zip") + "|" +
                    MimeTypes.GetMimeTypeByExtension("msg") + "|" +
                    MimeTypes.GetMimeTypeByExtension("eml")),
            };
        }
    }

    [TestFixture]
    [Category("integration_full")]
    public class integration_attachments_mail_with_zip_and_other_mail : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {

        [Test]
        public async void verify_email_count_file_verification()
        {

            _sutBase = new AttachmentOutOfProcessJob();
            PrepareJob();

            var handleClient = DocumentHandle.FromString("verify_eml");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToMsgWithComplexAttachment,
               handleClient,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            DateTime startWait = DateTime.Now;
            Int32 docCount;
            do
            {
                UpdateAndWait();
                docCount = _documentCollection.AsQueryable().Count();
                if (docCount == 11)
                {
                    //all attachment are unzipped correctly
                    var doc = _documentCollection.FindOneById(BsonValue.Create(handleClient.ToString()));
                    Assert.That(doc.Attachments, Has.Count.EqualTo(3), "primary document has wrong number of attachments");
                    return;
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            Assert.Fail("file from email not extracted correctly, I'm expecting 11 documetns but projection contains " + docCount + " documents");
        }



        protected override QueueInfo[] OnGetQueueInfo()
        {

            return new QueueInfo[]
            {
                new QueueInfo("attachments", mimeTypes : 
                    MimeTypes.GetMimeTypeByExtension("zip") + "|" +
                    MimeTypes.GetMimeTypeByExtension("msg") + "|" +
                    MimeTypes.GetMimeTypeByExtension("eml")),
            };
        }
    }
}
