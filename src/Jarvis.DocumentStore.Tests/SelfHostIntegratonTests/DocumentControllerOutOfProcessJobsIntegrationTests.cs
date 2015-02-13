using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using com.sun.javadoc;
using com.sun.tools.@internal.jxc.gen.config;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Jobs.OutOfProcessPollingJobs;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Jobs.Jobs;
using Jarvis.DocumentStore.Tests.JobTests;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using Jarvis.Framework.Kernel.MultitenantSupport;
using Jarvis.Framework.Shared.MultitenantSupport;
using Jarvis.Framework.TestHelpers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NUnit.Framework;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using DocumentFormat = Jarvis.DocumentStore.Client.Model.DocumentFormat;
using Jarvis.DocumentStore.Core.Support;
using Jarvis.DocumentStore.Core.Storage;
using System.Drawing;
using Jarvis.DocumentStore.Core.Jobs.QueueManager;
using Castle.Windsor;
using Jarvis.DocumentStore.Tests.ProjectionTests;

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
        protected DocumentStoreBootstrapper _documentStoreService;
        protected DocumentStoreServiceClient _documentStoreClient;
        protected MongoCollection<DocumentReadModel> _documents;
        protected DocumentStoreTestConfigurationForPollQueue _config;
        protected IBlobStore _blobStore;
        protected AbstractOutOfProcessPollerFileJob _sutBase;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _config = new DocumentStoreTestConfigurationForPollQueue(OnGetQueueInfo());

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
            _blobStore = tenant.Container.Resolve<IBlobStore>();
        }

        protected abstract QueueInfo[] OnGetQueueInfo();

        protected void PrepareJob(String handle = "TESTHANDLE", AbstractOutOfProcessPollerFileJob testJob = null)
        {
            var job = testJob ?? _sutBase;
            job.DocumentStoreConfiguration = _config;
            job.Logger = new TestLogger(LoggerLevel.Error);
            job.ConfigService = new ConfigService();
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

            var handle = DocumentHandle.FromString("verify_tika_job");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToWordDocument,
               DocumentHandle.FromString("verify_tika_job"),
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            DateTime startWait = DateTime.Now;
            DocumentReadModel document;
            do
            {
                Thread.Sleep(300);
                document = _documents.AsQueryable()
                    .SingleOrDefault(d => d.Handles.Contains(new Core.Model.DocumentHandle("verify_tika_job")));
                if (document != null &&
                    document.Formats.ContainsKey(new Core.Domain.Document.DocumentFormat("tika")))
                {
                    //Test is ok, document found
                    return;
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < 5000);

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
            do
            {
                Thread.Sleep(300);
                document = _documents.AsQueryable()
                    .SingleOrDefault(d => d.Handles.Contains(new Core.Model.DocumentHandle("verify_tika_job")));
                if (document != null &&
                    document.Formats.ContainsKey(new Core.Domain.Document.DocumentFormat("content")))
                {
                    //now we want to verify if content is set correctly
                    var client = new DocumentStoreServiceClient(TestConfig.ServerAddress, TestConfig.Tenant);
                    var content = await client.GetContentAsync(handle);
                    Assert.That(content.Pages.Length, Is.EqualTo(1));
                    Assert.That(content.Pages[0].Content, Contains.Substring("word document").IgnoreCase);
                    return;
                }
    
            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < 5000);

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
            sut.DocumentStoreConfiguration = _config;
            sut.Logger = new TestLogger(LoggerLevel.Error);
            sut.ConfigService = new ConfigService();
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
                Thread.Sleep(300);
                document = _documents.AsQueryable()
                    .SingleOrDefault(d => d.Handles.Contains(new Core.Model.DocumentHandle("verify_img_resize_job")));
                if (document != null &&
                    document.Formats.ContainsKey(thumbSmallFormat) &&
                    document.Formats.ContainsKey(thumbLargeFormat))
                {
                    var blobId = document.Formats[thumbSmallFormat].BlobId;
                    var file = _blobStore.GetDescriptor(blobId);
                    Assert.That(file.FileNameWithExtension.ToString(), Is.EqualTo("small.png"));
                    var downloadedImage = _blobStore.Download(blobId, Path.GetTempPath());
                    using (var image = Image.FromFile(downloadedImage)) 
                    {
                        Assert.That(image.Height, Is.EqualTo(200));
                    }

                     blobId = document.Formats[thumbLargeFormat].BlobId;
                    file = _blobStore.GetDescriptor(blobId);
                    Assert.That(file.FileNameWithExtension.ToString(), Is.EqualTo("large.png"));
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
                Thread.Sleep(300);
                document = _documents.AsQueryable()
                    .SingleOrDefault(d => d.Handles.Contains(new Core.Model.DocumentHandle("verify_chain_for_email")));
                if (document != null &&
                    document.Formats.ContainsKey(emailFormat))
                {
                   
                    return; //test is good
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < 5000);

            Assert.Fail("expected formats not found");
        }

        HtmlToPdfOutOfProcessJob _htmlSut;

        [Test]
        public async void verify_full_chain_for_email_and_html_zip()
        {
            _sutBase = sut = new AnalyzeEmailOutOfProcessJob();
            PrepareJob();

            HtmlToPdfOutOfProcessJob htmlSut = new HtmlToPdfOutOfProcessJob();
            PrepareJob(testJob : htmlSut);

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
                Thread.Sleep(300);
                document = _documents.AsQueryable()
                    .SingleOrDefault(d => d.Handles.Contains(new Core.Model.DocumentHandle("verify_chain_for_email")));
                if (document != null &&
                    document.Formats.ContainsKey(emailFormat) &&
                    document.Formats.ContainsKey(pdfFormat))
                {

                    return; //test is good
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < 5000);

            Assert.Fail("expected formats not found");
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
}
