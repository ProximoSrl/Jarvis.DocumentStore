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
using CQRS.Shared.MultitenantSupport;
using CQRS.Shared.ReadModel;
using CQRS.TestHelpers;
using CQRS.Tests.DomainTests;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Jobs;
using Jarvis.DocumentStore.Core.Jobs.OutOfProcessPollingJobs;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Tests.JobTests;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NUnit.Framework;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using DocumentFormat = Jarvis.DocumentStore.Client.Model.DocumentFormat;
using Jarvis.DocumentStore.Core.Support;

// ReSharper disable InconsistentNaming
namespace Jarvis.DocumentStore.Tests.SelfHostIntegratonTests
{
    /// <summary>
    /// test of controller when it works with pull model test.
    /// </summary>
    [TestFixture]
    public class DocumentControllerOutOfProcessJobsIntegrationTests
    {
        DocumentStoreBootstrapper _documentStoreService;
        private DocumentStoreServiceClient _documentStoreClient;
        private MongoCollection<DocumentReadModel> _documents;
        private DocumentStoreTestConfigurationForPollQueue _config;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _config = new DocumentStoreTestConfigurationForPollQueue();
            MongoDbTestConnectionProvider.DropAll();
            _config.ServerAddress = TestConfig.ServerAddress;
            _documentStoreService = new DocumentStoreBootstrapper();
            _documentStoreService.Start(_config);
            _documentStoreClient = new DocumentStoreServiceClient(
                TestConfig.ServerAddress,
                TestConfig.Tenant
            );
            _documents = MongoDbTestConnectionProvider.ReadModelDb.GetCollection<DocumentReadModel>("rm.Document");
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            _documentStoreService.Stop();
            BsonClassMapHelper.Clear();
        }

        [Test]
        public async void verify_tika_job()
        {
            OutOfProcessTikaNetJob sut = new OutOfProcessTikaNetJob();
            sut.DocumentStoreConfiguration = _config;
            sut.Logger = new TestLogger(LoggerLevel.Debug);
            sut.ConfigService = new ConfigService();
            sut.Start(new List<string>() { TestConfig.ServerAddress.AbsoluteUri });

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




    }
}
