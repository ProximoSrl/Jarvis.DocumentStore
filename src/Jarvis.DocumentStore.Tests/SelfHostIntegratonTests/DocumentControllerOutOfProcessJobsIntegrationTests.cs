extern alias tika;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Jobs.Email;
using Jarvis.DocumentStore.Jobs.HtmlZipOld;
using Jarvis.DocumentStore.Jobs.ImageResizer;
using tika::Jarvis.DocumentStore.Jobs.Tika;
using Jarvis.DocumentStore.JobsHost.Helpers;
using Jarvis.DocumentStore.JobsHost.Support;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using Jarvis.Framework.Kernel.MultitenantSupport;
using Jarvis.Framework.Kernel.ProjectionEngine;
using Jarvis.Framework.Shared.MultitenantSupport;
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

using NSubstitute;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;

using Jarvis.Framework.Shared.Helpers;
using Jarvis.DocumentStore.Jobs.PdfComposer;
using System.Reflection;

#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.
#pragma warning disable S101 // Types should be named in camel case

// ReSharper disable InconsistentNaming
namespace Jarvis.DocumentStore.Tests.SelfHostIntegratonTests
{
    /// <summary>
    /// Test of jobs executed out of process. This tests execute a full 
    /// 1) reset
    /// 2) upload document and wait for specific queue to process the docs
    /// 3) verify the outcome.
    /// </summary>
    //[TestFixture("v1")] //Uncomment this if you want to test with old projection engine
    [TestFixture("v3")]
    [Category("Integration_full")]
    //[Explicit("This integration test is slow because it wait for polling")]
    public abstract class DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public const int MaxTimeout = 10000;

        protected DocumentStoreBootstrapper _documentStoreService;
        protected DocumentStoreServiceClient _documentStoreClient;
        protected IMongoCollection<DocumentDescriptorReadModel> _documentDescriptorCollection;
        protected IMongoCollection<DocumentReadModel> _documentCollection;
        protected IMongoCollection<StreamReadModel> _streamCollection;
        protected IMongoCollection<QueuedJob> _tikaQueue;
        protected IMongoCollection<StreamCheckpoint> _queueCheckpoint;
        protected DocumentStoreTestConfigurationForPollQueue _config;
        protected JobsHostConfiguration _jobsHostConfiguration;
        protected IBlobStore _blobStore;
        protected AbstractOutOfProcessPollerJob _sutBase;

        private ITriggerProjectionsUpdate _projections;

        protected string _engineVersion;

        protected DocumentControllerOutOfProcessJobsIntegrationTestsBase(String engineVersion)
        {
            _engineVersion = engineVersion;
        }

        protected async Task UpdateAndWait()
        {
            await _projections.UpdateAndWait();
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            try
            {
                UdpAppender.AppendToConfiguration();
                _config = new DocumentStoreTestConfigurationForPollQueue(OnGetQueueInfo(), _engineVersion);
                _jobsHostConfiguration = new JobsHostConfiguration();
                MongoDbTestConnectionProvider.DropTenant(TestConfig.Tenant);
                _config.SetTestAddress(TestConfig.TestHostServiceAddress);
                _documentStoreService = new DocumentStoreBootstrapper();
                _documentStoreService.Start(_config);
                _documentStoreClient = new DocumentStoreServiceClient(
                    TestConfig.ServerAddress,
                    TestConfig.Tenant
                );
                _documentDescriptorCollection = MongoDbTestConnectionProvider.ReadModelDb.GetCollection<DocumentDescriptorReadModel>("rm.DocumentDescriptor");
                _documentCollection = MongoDbTestConnectionProvider.ReadModelDb.GetCollection<DocumentReadModel>("rm.Document");
                _streamCollection = MongoDbTestConnectionProvider.ReadModelDb.GetCollection<StreamReadModel>("rm.Stream");

                _tikaQueue = MongoDbTestConnectionProvider.QueueDb.GetCollection<QueuedJob>("queue.tika");
                _queueCheckpoint = MongoDbTestConnectionProvider.QueueDb.GetCollection<StreamCheckpoint>("stream.checkpoints");

                TenantContext.Enter(new TenantId(TestConfig.Tenant));
                var tenant = ContainerAccessor.Instance.Resolve<TenantManager>().Current;
                _projections = tenant.Container.Resolve<ITriggerProjectionsUpdate>();
                _blobStore = tenant.Container.Resolve<IBlobStore>();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        protected abstract QueueInfo[] OnGetQueueInfo();

        protected void PrepareJob(String handle = "TESTHANDLE", AbstractOutOfProcessPollerJob testJob = null)
        {
            var job = testJob ?? _sutBase;
            job.JobsHostConfiguration = _jobsHostConfiguration;
            job.Logger = new TestLogger(LoggerLevel.Error);
            OnJobPreparing(job);
            job.Start(new List<string>() { TestConfig.ServerAddress.AbsoluteUri }, handle);
        }

        protected virtual void OnJobPreparing(AbstractOutOfProcessPollerJob job)
        {
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _documentStoreService.Stop();
            BsonClassMapHelper.Clear();
            OnStop();
        }

        protected virtual void OnStop()
        {
            _sutBase?.Stop();
        }
    }

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class Integration_out_of_process_tika : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public Integration_out_of_process_tika(String engineVersion) : base(engineVersion)
        {
        }

        [Test]
        public async Task Verify_tika_job()
        {
            _sutBase = new OutOfProcessTikaNetJob(
                new ContentFormatBuilder(new ContentFilterManager(null)),
                new ContentFilterManager(null));
            PrepareJob();

            var handleCore = new Jarvis.DocumentStore.Core.Model.DocumentHandle("Verify_tika_job");
            var handleClient = new DocumentHandle("Verify_tika_job");
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
                await UpdateAndWait();
                documentDescriptor = _documentDescriptorCollection.AsQueryable()
                    .SingleOrDefault(d => d.Documents.Contains(handleCore));
                if (documentDescriptor != null
                    && documentDescriptor.Formats.ContainsKey(format))
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

        [Test]
        public async Task Verify_tika_job_on_htmlzip()
        {
            _sutBase = new OutOfProcessTikaNetJob(
                new ContentFormatBuilder(new ContentFilterManager(null)),
                new ContentFilterManager(null));
            PrepareJob();

            var handleCore = new Jarvis.DocumentStore.Core.Model.DocumentHandle("Verify_tika_job_with_htmlzip");
            var handleClient = new DocumentHandle("Verify_tika_job_with_htmlzip");
            var zipped = _documentStoreClient.ZipHtmlPage(TestConfig.PathToHtml);

            await _documentStoreClient.UploadAsync(
               zipped,
               handleClient,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            DateTime startWait = DateTime.Now;
            DocumentDescriptorReadModel documentDescriptor;
            var formats = new[] { new DocumentFormat("tika"), new DocumentFormat("content") };
            do
            {
                await UpdateAndWait();
                documentDescriptor = _documentDescriptorCollection.AsQueryable()
                    .SingleOrDefault(d => d.Documents.Contains(handleCore));
                if (documentDescriptor != null &&
                    formats.All(f => documentDescriptor.Formats.ContainsKey(f)))
                {
                    //Document found, but we want to be sure that the fileName of the format is correct.
                    var tikaFormatInfo = documentDescriptor.Formats[formats[0]];
                    var blob = _blobStore.GetDescriptor(tikaFormatInfo.BlobId);
                    Assert.That(
                        blob.FileNameWithExtension.ToString(),
                        Is.EqualTo(Path.GetFileNameWithoutExtension(TestConfig.PathToHtml) + ".tika.html"),
                        "File name is wrong, we expect the same file name with extension .tika.html");

                    var contentFormatInfo = documentDescriptor.Formats[formats[1]];
                    var contentFile = _blobStore.Download(contentFormatInfo.BlobId, Path.GetTempPath());
                    var content = File.ReadAllText(contentFile);

                    Assert.That(content.Contains("previous post we introduced Jarvis"));
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

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class Integration_out_of_process_tika_long_name : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public Integration_out_of_process_tika_long_name(String engineVersion) : base(engineVersion)
        {
        }

        [Test]
        public async Task Verify_tika_job_with_long_name()
        {
            _sutBase = new OutOfProcessTikaNetJob(
                new ContentFormatBuilder(new ContentFilterManager(null)),
                new ContentFilterManager(null));
            PrepareJob();

            String longFileName = Path.Combine(
               Path.GetTempPath(),
               "_lfn" + new string('X', 240) + ".pdf");
            if (!File.Exists(longFileName))
            {
                File.Copy(TestConfig.PathToWordDocument, longFileName);
            }

            var handleCore = new Jarvis.DocumentStore.Core.Model.DocumentHandle("Verify_tika_job");
            var handleClient = new DocumentHandle("Verify_tika_job");
            await _documentStoreClient.UploadAsync(
               longFileName,
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
                await UpdateAndWait();
                documentDescriptor = _documentDescriptorCollection.AsQueryable()
                    .SingleOrDefault(d => d.Documents.Contains(handleCore));
                if (documentDescriptor != null &&
                    documentDescriptor.Formats.ContainsKey(format))
                {
                    //Document found
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

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class A_Integration_out_of_process_tika_password : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public A_Integration_out_of_process_tika_password(String engineVersion)
            : base(engineVersion)
        {
        }

        protected override void OnJobPreparing(AbstractOutOfProcessPollerJob job)
        {
            var subPassword = NSubstitute.Substitute.For<IClientPasswordSet>();
            subPassword.GetPasswordFor(Arg.Any<String>()).Returns(new[] { "jarvistest" });
            job.ClientPasswordSet = subPassword;
        }

        [Test]
        public async Task A_Verify_tika_job_with_password()
        {
            _sutBase = new OutOfProcessTikaNetJob(
                new ContentFormatBuilder(new ContentFilterManager(null)),
                new ContentFilterManager(null));
            PrepareJob();

            var handleCore = new Jarvis.DocumentStore.Core.Model.DocumentHandle("Verify_tika_job");
            var handleClient = new DocumentHandle("Verify_tika_job");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToPasswordProtectedPdf,
               handleClient,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            DateTime startWait = DateTime.Now;
            DocumentDescriptorReadModel documentDescriptor;
            var tikaFormat = new DocumentFormat("tika");
            var contentFormat = new DocumentFormat("content");
            do
            {
                await UpdateAndWait();
                documentDescriptor = _documentDescriptorCollection.AsQueryable()
                    .SingleOrDefault(d => d.Documents.Contains(handleCore));
                if (documentDescriptor != null
                    && documentDescriptor.Formats.ContainsKey(tikaFormat)
                    && documentDescriptor.Formats.ContainsKey(contentFormat))
                {
                    //Document found, but we want to be sure that the fileName of the format is correct.
                    var formatInfo = documentDescriptor.Formats[tikaFormat];
                    var blob = _blobStore.GetDescriptor(formatInfo.BlobId);
                    Assert.That(
                        blob.FileNameWithExtension.ToString(),
                        Is.EqualTo(Path.GetFileNameWithoutExtension(TestConfig.PathToPasswordProtectedPdf) + ".tika.html"),
                        "File name is wrong, we expect the same file name with extension .tika.html");

                    var contentFormatInfo = documentDescriptor.Formats[contentFormat];

                    var content = _blobStore.Download(contentFormatInfo.BlobId, Path.GetTempPath());
                    var contentString = File.ReadAllText(content);
                    Assert.That(contentString, Contains.Substring("Questo documento è protetto da password."));
                    return;
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            if (_sutBase.ClientPasswordSet is NullClientPasswordSet)
                Assert.Fail("Password manager not correctly set to decrypt passwords");
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

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class A_Integration_out_of_process_tika_multiple_password : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public A_Integration_out_of_process_tika_multiple_password(String engineVersion)
            : base(engineVersion)
        {
        }

        protected override void OnJobPreparing(AbstractOutOfProcessPollerJob job)
        {
            var subPassword = NSubstitute.Substitute.For<IClientPasswordSet>();
            subPassword.GetPasswordFor(Arg.Any<String>()).Returns(new[] { "wrongPassword", "jarvistest" });
            job.ClientPasswordSet = subPassword;
        }

        [Test]
        public async Task A_Verify_tika_job_with_multiple_password()
        {
            _sutBase = new OutOfProcessTikaNetJob(
                new ContentFormatBuilder(new ContentFilterManager(null)),
                new ContentFilterManager(null));
            PrepareJob();

            var handleCore = new Core.Model.DocumentHandle("Verify_tika_job_multiple");
            var handleClient = new DocumentHandle("Verify_tika_job_multiple");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToPasswordProtectedPdf,
               handleClient,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            DateTime startWait = DateTime.Now;
            DocumentDescriptorReadModel documentDescriptor;
            var tikaFormat = new DocumentFormat("tika");
            var contentFormat = new DocumentFormat("content");
            do
            {
                await UpdateAndWait();
                documentDescriptor = _documentDescriptorCollection.AsQueryable()
                    .SingleOrDefault(d => d.Documents.Contains(handleCore));
                if (documentDescriptor != null
                    && documentDescriptor.Formats.ContainsKey(tikaFormat)
                    && documentDescriptor.Formats.ContainsKey(contentFormat))
                {
                    //Document found, but we want to be sure that the fileName of the format is correct.
                    var formatInfo = documentDescriptor.Formats[tikaFormat];
                    var blob = _blobStore.GetDescriptor(formatInfo.BlobId);
                    Assert.That(
                        blob.FileNameWithExtension.ToString(),
                        Is.EqualTo(Path.GetFileNameWithoutExtension(TestConfig.PathToPasswordProtectedPdf) + ".tika.html"),
                        "File name is wrong, we expect the same file name with extension .tika.html");

                    var contentFormatInfo = documentDescriptor.Formats[contentFormat];

                    var content = _blobStore.Download(contentFormatInfo.BlobId, Path.GetTempPath());
                    var contentString = File.ReadAllText(content);
                    Assert.That(contentString, Contains.Substring("Questo documento è protetto da password."));
                    return;
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            if (_sutBase.ClientPasswordSet is NullClientPasswordSet)
                Assert.Fail("Password manager not correctly set to decrypt passwords");

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

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class Integration_out_of_process_no_multiple_schedule : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public Integration_out_of_process_no_multiple_schedule(String engineVersion)
            : base(engineVersion)
        {
        }

        /// <summary>
        /// When duplicate document is added to DS it generates two record in the StreamReadModel
        /// but <see cref="QueueManager" /> needs to generate only one job. 
        /// </summary>
        [Test]
        public async Task Verify_no_multiple_schedule_for_de_duplicated_document()
        {
            _sutBase = new OutOfProcessTikaNetJob(
                new ContentFormatBuilder(new ContentFilterManager(null)),
                new ContentFilterManager(null));
            PrepareJob();

            var handleClient1 = new DocumentHandle("Verify_tika_job1");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToWordDocument,
               handleClient1,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            var handleClient2 = new DocumentHandle("Verify_tika_job2");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToWordDocument,
               handleClient2,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            await UpdateAndWait();
            var allStream = _streamCollection.FindAll();
            var maxCheckpoint = allStream.ToList().Select(s => s.Id).Max();
            StreamCheckpoint checkpoint;
            DateTime startWait = DateTime.Now;
            do
            {
                checkpoint = _queueCheckpoint.FindOneById(BsonValue.Create(TestConfig.Tenant));
                if (checkpoint == null || checkpoint.Checkpoint < maxCheckpoint)
                {
                    Thread.Sleep(200); //wait for queue manager to read all stream
                }
            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            if (checkpoint == null || checkpoint.Checkpoint < maxCheckpoint)
            {
                Assert.Fail("Queue Manager unable to process all stream readmodel record!!");
            }
            Assert.That(_tikaQueue.AsQueryable().Count(), Is.EqualTo(1), "Job generated for duplicate document, error");
        }

        protected override QueueInfo[] OnGetQueueInfo()
        {
            return new QueueInfo[]
            {
                new QueueInfo("tika", "original", ""),
            };
        }
    }

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class Integration_out_of_process_tika_content : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public Integration_out_of_process_tika_content(String engineVersion)
            : base(engineVersion)
        {
        }

        [Test]
        public async Task Verify_tika_set_content()
        {
            _sutBase = new OutOfProcessTikaNetJob(
                new ContentFormatBuilder(new ContentFilterManager(null)),
                new ContentFilterManager(null));
            PrepareJob();

            var handle = DocumentHandle.FromString("Verify_tika_job");
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
                await UpdateAndWait();
                documentDescriptor = _documentDescriptorCollection.AsQueryable()
                    .SingleOrDefault(d => d.Documents.Contains(new Core.Model.DocumentHandle("Verify_tika_job")));
                if (documentDescriptor?.Formats.ContainsKey(format) == true)
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

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class Integration_out_of_process_image : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public Integration_out_of_process_image(String engineVersion)
            : base(engineVersion)
        {
        }

        [Test]
        public async Task Verify_image_resizer_job()
        {
            ImageResizePollerOutOfProcessJob sut = new ImageResizePollerOutOfProcessJob();
            sut.JobsHostConfiguration = _jobsHostConfiguration;
            sut.Logger = new TestLogger(LoggerLevel.Error);
            sut.Start(new List<string>() { TestConfig.ServerAddress.AbsoluteUri }, "TESTHANDLE");
            DocumentHandle handle = DocumentHandle.FromString("Verify_img_resize_job");
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
                await UpdateAndWait();
                documentDescriptor = _documentDescriptorCollection.AsQueryable()
                    .SingleOrDefault(d => d.Documents.Contains(new Core.Model.DocumentHandle("Verify_img_resize_job")));
                if (documentDescriptor != null
                    && documentDescriptor.Formats.ContainsKey(thumbSmallFormat)
                    && documentDescriptor.Formats.ContainsKey(thumbLargeFormat))
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

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class Integration_out_of_process_eml : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public Integration_out_of_process_eml(String engineVersion)
            : base(engineVersion)
        {
        }

        [Test]
        public async Task Verify_chain_for_email()
        {
            _sutBase = new AnalyzeEmailOutOfProcessJob();
            PrepareJob();

            DocumentHandle handle = DocumentHandle.FromString("Verify_chain_for_email");
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
                await UpdateAndWait();
                documentDescriptor = _documentDescriptorCollection.AsQueryable()
                    .SingleOrDefault(d => d.Documents.Contains(new Core.Model.DocumentHandle("Verify_chain_for_email")));
                if (documentDescriptor != null
                    && documentDescriptor.Formats.ContainsKey(emailFormat))
                {
                    return; //test is good
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            Assert.Fail("expected formats not found");
        }

        protected override QueueInfo[] OnGetQueueInfo()
        {
            return new QueueInfo[]
            {
                new QueueInfo("email", "", "eml|msg"),
            };
        }
    }

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class Integration_out_of_process_eml_chain : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        private HtmlToPdfOutOfProcessJobOld _htmlSut;

        public Integration_out_of_process_eml_chain(String engineVersion)
            : base(engineVersion)
        {
        }

        [Test]
        public async Task Verify_full_chain_for_email_and_html_zipOld()
        {
            _sutBase = new AnalyzeEmailOutOfProcessJob();
            PrepareJob();

            _htmlSut = new HtmlToPdfOutOfProcessJobOld();
            PrepareJob(testJob: _htmlSut);

            DocumentHandle handle = DocumentHandle.FromString("Verify_chain_for_email");
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
                await UpdateAndWait();
                documentDescriptor = _documentDescriptorCollection.AsQueryable()
                    .SingleOrDefault(d => d.Documents.Contains(new Core.Model.DocumentHandle("Verify_chain_for_email")));
                if (documentDescriptor != null
                    && documentDescriptor.Formats.ContainsKey(emailFormat)
                    && documentDescriptor.Formats.ContainsKey(pdfFormat))
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
            _htmlSut?.Stop();
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

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class Integration_out_of_process_html_to_pdf : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public Integration_out_of_process_html_to_pdf(String engineVersion)
            : base(engineVersion)
        {
            TestLogger.GlobalEnableStartScope();
        }

        public async Task<Boolean> Verify_htmlToPdf_base<T>(String testFile) where T : AbstractOutOfProcessPollerJob, new()
        {
            _sutBase = new T();
            PrepareJob();

            DocumentHandle handle = DocumentHandle.FromString("Verify_chain_for_htmlzip");
            await _documentStoreClient.UploadAsync(
               testFile,
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
                await UpdateAndWait();
                documentDescriptor = _documentDescriptorCollection.AsQueryable()
                    .SingleOrDefault(d => d.Documents.Contains(new Core.Model.DocumentHandle("Verify_chain_for_htmlzip")));
                if (documentDescriptor != null
                    && documentDescriptor.Formats.ContainsKey(pdfFormat))
                {
                    return true; //test is good
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            return false;
        }

        [Test]
        public async Task Verify_html_zipped_ToPdf_old()
        {
            var client = new DocumentStoreServiceClient(TestConfig.ServerAddress, TestConfig.Tenant);
            var zipped = client.ZipHtmlPage(TestConfig.PathToHtml);
            Assert.That(await Verify_htmlToPdf_base<HtmlToPdfOutOfProcessJobOld>(zipped), "Format Pdf not found");
        }

        [Test]
        public async Task Verify_html_plain_ToPdf_old()
        {
            Assert.That(await Verify_htmlToPdf_base<HtmlToPdfOutOfProcessJobOld>(TestConfig.PathToSimpleHtmlFile), "Format Pdf not found");
        }

        protected override QueueInfo[] OnGetQueueInfo()
        {
            return new QueueInfo[]
            {
                new QueueInfo("htmlzip", "", "htmlzip|ezip"),
            };
        }
    }

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class Integration_out_of_process_office_to_pdf : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public Integration_out_of_process_office_to_pdf(String engineVersion)
            : base(engineVersion)
        {
        }

        /// <summary>
        /// Pay attention, this test for some unknown problem of library loading from IUNO does
        /// not execute correctly alone. It runs correctly only if executed after other tests.
        /// Dunno why, 1 hour spent by alkampfer and guardian without any clue.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task Verify_office_job()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Console.WriteLine("Current Directory Is: " + Environment.CurrentDirectory);

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
                await UpdateAndWait();
                documentDescriptor = _documentDescriptorCollection.AsQueryable()
                    .SingleOrDefault(d => d.Documents.Contains(handleServer));
                if (documentDescriptor != null
                    && documentDescriptor.Formats.ContainsKey(format))
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

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class Integration_attachments_queue_multiple_zip : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public Integration_attachments_queue_multiple_zip(String engineVersion)
            : base(engineVersion)
        {
        }

        [Test]
        public async Task Verify_nested_zip_count_file_verification()
        {
            _sutBase = new AttachmentOutOfProcessJob(new SevenZipExtractorFunctions());
            PrepareJob();

            var handleClient = DocumentHandle.FromString("Verify_nested_zip");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToZipFileThatContainsOtherZip,
               handleClient,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            DateTime startWait = DateTime.Now;
            int docCount;
            do
            {
                await UpdateAndWait();
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

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class Integration_attachments_queue_singleZip : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public Integration_attachments_queue_singleZip(String engineVersion)
            : base(engineVersion)
        {
        }

        [Test]
        public async Task Verify_single_zip_count_file_verification()
        {
            _sutBase = new AttachmentOutOfProcessJob(new SevenZipExtractorFunctions());
            PrepareJob();

            var handleClient = DocumentHandle.FromString("Verify_zip");
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
                await UpdateAndWait();
                docCount = _documentCollection.AsQueryable().Count();
                if (docCount == 4)
                {
                    var doc = _documentDescriptorCollection.Find(
                        Builders<DocumentDescriptorReadModel>.Filter.Eq("Documents", handleClient.ToString()))
                        .Single();
                    Assert.That(doc.Attachments.Select(a => a.Handle), Is.EquivalentTo(new[] {
                        new Core.Model.DocumentHandle("content_zip_1"),
                        new Core.Model.DocumentHandle("content_zip_2"),
                        new Core.Model.DocumentHandle("content_zip_3")
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

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class Integration_attachment_then_same_attach_inside_externa_attach : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public Integration_attachment_then_same_attach_inside_externa_attach(String engineVersion)
            : base(engineVersion)
        {
        }

        [Test]
        public async Task Verify_attachment_chains()
        {
            _sutBase = new AttachmentOutOfProcessJob(new SevenZipExtractorFunctions());
            PrepareJob();

            var handleClient = DocumentHandle.FromString("child_zip");

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
                await UpdateAndWait();
                docCount = _documentDescriptorCollection.AsQueryable().Count();
                if (docCount == 4)
                {
                    break;
                }
            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            //now all attachment are unzipped.
            handleClient = DocumentHandle.FromString("containing_zip");
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
                await UpdateAndWait();
                docCount = _documentCollection.AsQueryable().Count();
                if (docCount == 8)
                {
                    //now I want to be sure that the whole set of attachments is correct
                    var fat = await _documentStoreClient.GetAttachmentsFatAsync(handleClient);
                    Assert.That(fat.Attachments, Has.Count.EqualTo(6));
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

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class Integration_handle_with_attachment_duplicated_then_first_deleted : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public Integration_handle_with_attachment_duplicated_then_first_deleted(String engineVersion)
            : base(engineVersion)
        {
        }

        [Test]
        public async Task Verify_deletion_original_handle()
        {
            _sutBase = new AttachmentOutOfProcessJob(new SevenZipExtractorFunctions());
            PrepareJob();

            var handleClient = DocumentHandle.FromString("main");
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
                await UpdateAndWait();
                docCount = _documentDescriptorCollection.AsQueryable().Count();
                if (docCount == 4)
                {
                    break;
                }
            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            //now all attachment are unzipped.
            var secondaryHandleClient = DocumentHandle.FromString("secondary");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToZipFile, //Same file it will be de-duplicated
               secondaryHandleClient,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            //now delete primary handle
            await _documentStoreClient.DeleteAsync(handleClient);

            await UpdateAndWait();

            //now I want to be sure that secondary handle still has attachments
            var fat = await _documentStoreClient.GetAttachmentsFatAsync(secondaryHandleClient);
            Assert.That(fat.Attachments, Has.Count.EqualTo(3));
            var allHandle = _documentCollection.CountDocuments(Builders<DocumentReadModel>.Filter.Empty);
            Assert.That(allHandle, Is.EqualTo(4)); //second handle with all attachments
        }

        protected override QueueInfo[] OnGetQueueInfo()
        {
            return new QueueInfo[]
            {
                new QueueInfo("attachments", mimeTypes : MimeTypes.GetMimeTypeByExtension("zip")),
            };
        }
    }

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class Integration_attachments_mail_with_attach : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public Integration_attachments_mail_with_attach(String engineVersion)
            : base(engineVersion)
        {
        }

        [Test]
        public async Task Verify_email_count_file_verification()
        {
            _sutBase = new AttachmentOutOfProcessJob(new SevenZipExtractorFunctions());
            PrepareJob();

            var handleClient = DocumentHandle.FromString("Verify_eml");
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
                await UpdateAndWait();
                docCount = _documentDescriptorCollection.AsQueryable().Count();
                if (docCount == 2)
                {
                    //all attachment are unzipped correctly
                    var doc = _documentDescriptorCollection.Find(
                    Builders<DocumentDescriptorReadModel>.Filter.Eq("Documents", handleClient.ToString()))
                    .Single();
                    Assert.That(doc.Attachments.Select(a => a.Handle), Is.EquivalentTo(new[] {
                        new Core.Model.DocumentHandle("attachment_email_1") }));
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

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class Integration_attachments_mail_with_zip_and_other_mail : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public Integration_attachments_mail_with_zip_and_other_mail(String engineVersion)
            : base(engineVersion)
        {
        }

        [Test]
        public async Task Verify_email_count_file_verification()
        {
            _sutBase = new AttachmentOutOfProcessJob(new SevenZipExtractorFunctions());
            PrepareJob();

            var handleClient = DocumentHandle.FromString("Verify_eml");
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
                await UpdateAndWait();
                docCount = _documentCollection.AsQueryable().Count();
                if (docCount == 10)
                {
                    //all attachment are unzipped correctly
                    var doc = _documentDescriptorCollection.Find(
                        Builders<DocumentDescriptorReadModel>.Filter.Eq("Documents", handleClient.ToString()))
                        .Single();
                    Assert.That(doc.Attachments, Has.Count.EqualTo(2), "primary document has wrong number of attachments");
                    return;
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            Assert.Fail("file from email not extracted correctly, I'm expecting 10 documetns but projection contains " + docCount + " documents");
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

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class Deletion_of_attachments_advanced : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public Deletion_of_attachments_advanced(String engineVersion)
            : base(engineVersion)
        {
        }

        [Test]
        public async Task Verify_deletion_original_handle_delete_attachments()
        {
            _sutBase = new AttachmentOutOfProcessJob(new SevenZipExtractorFunctions());
            PrepareJob();

            var handleClient = DocumentHandle.FromString("main");
            await _documentStoreClient.UploadAsync(
               TestConfig.PathToZipFile,
               handleClient,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );
            DateTime startWait = DateTime.Now;
            Int32 docDescriptorCount = 0;
            do
            {
                await UpdateAndWait();
                docDescriptorCount = _documentDescriptorCollection.AsQueryable().Count();
                if (docDescriptorCount == 4)
                {
                    break;
                }
            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            //now delete primary handle
            await _documentStoreClient.DeleteAsync(handleClient);

            await UpdateAndWait();

            //all attachment should be deleted
            docDescriptorCount = _documentDescriptorCollection.AsQueryable().Count();
            Assert.That(docDescriptorCount, Is.EqualTo(0));

            var docCount = _documentCollection.AsQueryable().Count();
            Assert.That(docCount, Is.EqualTo(0));
        }

        protected override QueueInfo[] OnGetQueueInfo()
        {
            return new QueueInfo[]
            {
                new QueueInfo("attachments", mimeTypes : MimeTypes.GetMimeTypeByExtension("zip")),
            };
        }
    }

    [TestFixture("v3")]
    [Category("Integration_full")]
    public class Verify_composition_of_pdf_file : DocumentControllerOutOfProcessJobsIntegrationTestsBase
    {
        public Verify_composition_of_pdf_file(String engineVersion)
            : base(engineVersion)
        {
        }

        [Test]
        public async Task Basic_composition()
        {
            _sutBase = new PdfComposerOutOfProcessJob();
            PrepareJob();

            var handle1 = DocumentHandle.FromString("h1");
            var handle2 = DocumentHandle.FromString("h2");

            var handleResult = DocumentHandle.FromString("result");

            await _documentStoreClient.UploadAsync(
               TestConfig.PathToDocumentPdf,
               handle1,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            await _documentStoreClient.UploadAsync(
               TestConfig.PathToDocumentCopyPdf,
               handle2,
               new Dictionary<string, object>{
                    { "callback", "http://localhost/demo"}
                }
            );

            await _documentStoreClient.ComposeDocumentsAsync(handleResult, handle1, handle2);

            DateTime startWait = DateTime.Now;
            Int32 docCount;
            do
            {
                await UpdateAndWait();
                docCount = _documentCollection.AsQueryable().Count();
                if (docCount == 3)
                {
                    //all attachment are unzipped correctly
                    _documentCollection.Find(
                        Builders<DocumentReadModel>.Filter.Eq("_id", handleResult.ToString())
                    ).Single();

                    return;
                }

            } while (DateTime.Now.Subtract(startWait).TotalMilliseconds < MaxTimeout);

            Assert.Fail("Pdf creator did not worked");
        }

        protected override QueueInfo[] OnGetQueueInfo()
        {
            return new QueueInfo[]
            {
                new QueueInfo("pdfComposer"),
            };
        }
    }
#pragma warning restore S101 // Types should be named in camel case
#pragma warning restore RCS1090 // Call 'ConfigureAwait(false)'.
}
