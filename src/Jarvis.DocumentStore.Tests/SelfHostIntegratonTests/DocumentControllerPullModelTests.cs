//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Net.Http;
//using System.Threading;
//using System.Threading.Tasks;
//using CQRS.Shared.MultitenantSupport;
//using CQRS.Shared.ReadModel;
//using CQRS.Tests.DomainTests;
//using Jarvis.DocumentStore.Client;
//using Jarvis.DocumentStore.Client.Model;
//using Jarvis.DocumentStore.Core.Domain.Document;
//using Jarvis.DocumentStore.Core.Jobs;
//using Jarvis.DocumentStore.Core.ReadModel;
//using Jarvis.DocumentStore.Host.Support;
//using Jarvis.DocumentStore.Tests.JobTests;
//using Jarvis.DocumentStore.Tests.PipelineTests;
//using Jarvis.DocumentStore.Tests.Support;
//using NUnit.Framework;
//using Quartz;
//using Quartz.Impl;
//using Quartz.Spi;
//using DocumentFormat = Jarvis.DocumentStore.Client.Model.DocumentFormat;
//using Jarvis.DocumentStore.Core.Support;

//// ReSharper disable InconsistentNaming
//namespace Jarvis.DocumentStore.Tests.SelfHostIntegratonTests
//{
//    /// <summary>
//    /// test of controller when it works with pull model test.
//    /// </summary>
//    [TestFixture]
//    public class DocumentControllerPullModelTests
//    {
//        DocumentStoreBootstrapper _documentStoreService;
//        private DocumentStoreServiceClient _documentStoreClient;

//        [TestFixtureSetUp]
//        public void TestFixtureSetUp()
//        {
//            var config = new DocumentStoreConfiguration();
//            config.IsApiServer = true;
//            config.IsWorker = false;
//            config.IsQueueManager = true;
//            config.IsReadmodelBuilder = true;

//            config.QuartzConnectionString = ConfigurationManager.ConnectionStrings["ds.quartz"].ConnectionString;
//            config.QueueConnectionString = ConfigurationManager.ConnectionStrings["ds.queue"].ConnectionString;
//            config.QueueInfoList = new Core.Jobs.QueueManager.QueueInfo[] { };

//            config.IsQueueManager = false;
//            config.TenantSettings.Add(new TestTenantSettings());

//            MongoDbTestConnectionProvider.DropTestsTenant();

//            _documentStoreService = new DocumentStoreBootstrapper(TestConfig.ServerAddress);
//            _documentStoreService.Start(config);
//            _documentStoreClient = new DocumentStoreServiceClient(
//                TestConfig.ServerAddress, 
//                TestConfig.Tenant
//            );
//        }

//        [TestFixtureTearDown]
//        public void TestFixtureTearDown()
//        {
//            _documentStoreService.Stop();
//            BsonClassMapHelper.Clear();
//        }

//        [Test]
//        public async void should_upload_and_download_original_format()
//        {
//            await _documentStoreClient.UploadAsync(
//                TestConfig.PathToDocumentPdf,
//                DocumentHandle.FromString("Pdf_2"),
//                new Dictionary<string, object>{
//                    { "callback", "http://localhost/demo"}
//                }
//            );

//            // waits for storage
//            Thread.Sleep(2000);

//            using (var reader = _documentStoreClient.OpenRead(DocumentHandle.FromString("Pdf_2")))
//            {
//                using (var downloaded = new MemoryStream())
//                using (var uploaded = new MemoryStream())
//                {
//                    using (var fileStream = File.OpenRead(TestConfig.PathToDocumentPdf))
//                    {
//                        await fileStream.CopyToAsync(uploaded);
//                    }
//                    await (await reader.ReadStream).CopyToAsync(downloaded);

//                    Assert.IsTrue(CompareMemoryStreams(uploaded, downloaded));
//                }
//            }
//        }

        
//    }
//}
