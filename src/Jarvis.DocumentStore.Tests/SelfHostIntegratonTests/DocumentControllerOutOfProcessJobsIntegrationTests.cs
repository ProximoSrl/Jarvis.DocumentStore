using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Jobs.OutOfProcessPollingJobs;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Services;
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
using Jarvis.DocumentStore.Jobs.Office;

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
        protected MongoCollection<DocumentReadModel> _documents;
        protected DocumentStoreTestConfigurationForPollQueue _config;
        protected JobsHostConfiguration _jobsHostConfiguration;
        protected IBlobStore _blobStore;
        protected AbstractOutOfProcessPollerJob _sutBase;

        private ITriggerProjectionsUpdate _projections;

        protected void UpdateAndWait()
        {
            _projections.UpdateAndWait();
            Thread.Sleep(500); //update and wait returns immediately if there are no other stuff to dispatch.
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
            _documents = MongoDbTestConnectionProvider.ReadModelDb.GetCollection<DocumentReadModel>("rm.Document");
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
            DocumentReadModel document;
            var format = new Core.Domain.Document.DocumentFormat("tika");
            do
            {
                UpdateAndWait();
                document = _documents.AsQueryable()
                    .SingleOrDefault(d => d.Handles.Contains(handleCore));
                if (document != null &&
                    document.Formats.ContainsKey(format))
                {
                    //Document found, but we want to be sure that the fileName of the format is correct.
                    var formatInfo = document.Formats[format];
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
            DocumentReadModel document;
            var format = new Core.Domain.Document.DocumentFormat("content");
            do
            {
                UpdateAndWait();
                document = _documents.AsQueryable()
                    .SingleOrDefault(d => d.Handles.Contains(new Core.Model.DocumentHandle("verify_tika_job")));
                if (document != null &&
                    document.Formats.ContainsKey(format))
                {
                    //now we want to verify if content is set correctly
                    var client = new DocumentStoreServiceClient(TestConfig.ServerAddress, TestConfig.Tenant);
                    var content = await client.GetContentAsync(handle);
                    Assert.That(content.Pages.Length, Is.EqualTo(1));
                    Assert.That(content.Pages[0].Content, Contains.Substring("word document").IgnoreCase);

                    var formatInfo = document.Formats[format];
                    var blob = _blobStore.GetDescriptor(formatInfo.BlobId);
                    Assert.That(
                        blob.FileNameWithExtension.ToString(),
                        Is.EqualTo(Path.GetFileNameWithoutExtension(TestConfig.PathToWordDocument) + ".content"),
                        "File name is wrong, we expect the same file name with extension .content");

                    return;
                }
    
            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < 50000);

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
    public class integration_out_of_process_image  : DocumentControllerOutOfProcessJobsIntegrationTestsBase
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
            DocumentReadModel document;
            var thumbSmallFormat = new Core.Domain.Document.DocumentFormat("thumb.small");
            var thumbLargeFormat = new Core.Domain.Document.DocumentFormat("thumb.large");
            do
            {
                UpdateAndWait();
                document = _documents.AsQueryable()
                    .SingleOrDefault(d => d.Handles.Contains(new Core.Model.DocumentHandle("verify_img_resize_job")));
                if (document != null &&
                    document.Formats.ContainsKey(thumbSmallFormat) &&
                    document.Formats.ContainsKey(thumbLargeFormat))
                {
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(TestConfig.PathToDocumentPng);
                    var blobId = document.Formats[thumbSmallFormat].BlobId;
                    var file = _blobStore.GetDescriptor(blobId);
                    Assert.That(file.FileNameWithExtension.ToString(), Is.EqualTo(fileNameWithoutExtension + ".small.png"));
                    var downloadedImage = _blobStore.Download(blobId, Path.GetTempPath());
                    using (var image = Image.FromFile(downloadedImage)) 
                    {
                        Assert.That(image.Height, Is.EqualTo(200));
                    }

                     blobId = document.Formats[thumbLargeFormat].BlobId;
                    file = _blobStore.GetDescriptor(blobId);
                    Assert.That(file.FileNameWithExtension.ToString(), Is.EqualTo(fileNameWithoutExtension + ".large.png"));
                    downloadedImage = _blobStore.Download(blobId, Path.GetTempPath());
                    using (var image = Image.FromFile(downloadedImage))
                    {
                        Assert.That(image.Height, Is.EqualTo(800));
                    }
                    return; //test is good
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < 5000);

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
            DocumentReadModel document;
            var emailFormat = new Core.Domain.Document.DocumentFormat("email");
            do
            {
                UpdateAndWait();
                document = _documents.AsQueryable()
                    .SingleOrDefault(d => d.Handles.Contains(new Core.Model.DocumentHandle("verify_chain_for_email")));
                if (document != null &&
                    document.Formats.ContainsKey(emailFormat))
                {
                   
                    return; //test is good
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            Assert.Fail("expected formats not found");
        }

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
            DocumentReadModel document;
            var emailFormat = new Core.Domain.Document.DocumentFormat("email");
            var pdfFormat = new Core.Domain.Document.DocumentFormat("Pdf");
            do
            {
                UpdateAndWait();
                document = _documents.AsQueryable()
                    .SingleOrDefault(d => d.Handles.Contains(new Core.Model.DocumentHandle("verify_chain_for_email")));
                if (document != null &&
                    document.Formats.ContainsKey(emailFormat) &&
                    document.Formats.ContainsKey(pdfFormat))
                {

                    return; //test is good
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            if (document == null)
                Assert.Fail("Missing document");

            Debug.WriteLine("document:\n{0}", (object)JsonConvert.SerializeObject(document, Formatting.Indented));

            if (!document.Formats.ContainsKey(emailFormat))
                Assert.Fail("Missing format: {0}", emailFormat);

            if (!document.Formats.ContainsKey(pdfFormat))
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
            DocumentReadModel document;
            var pdfFormat = DocumentFormats.Pdf;
            do
            {
                UpdateAndWait();
                document = _documents.AsQueryable()
                    .SingleOrDefault(d => d.Handles.Contains(new Core.Model.DocumentHandle("verify_chain_for_htmlzip")));
                if (document != null &&
                    document.Formats.ContainsKey(pdfFormat))
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
            DocumentReadModel document;
            var format = new Core.Domain.Document.DocumentFormat("Pdf");
            do
            {
                UpdateAndWait();
                document = _documents.AsQueryable()
                    .SingleOrDefault(d => d.Handles.Contains(handleServer));
                if (document != null &&
                    document.Formats.ContainsKey(format))
                {
                    //Document found, but we want to be sure that the fileName of the format is correct.
                    var formatInfo = document.Formats[format];
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
}
