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
    /// Test of jobs executed out of process.
    /// </summary>
    [TestFixture]
    //[Explicit("This integration test is slow because it wait for polling")]
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
            MongoDbTestConnectionProvider.DropTenant(TestConfig.Tenant);
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
            sut.Start(new List<string>() { TestConfig.ServerAddress.AbsoluteUri }, "TESTHANDLE");

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
            OutOfProcessTikaNetJob sut = new OutOfProcessTikaNetJob();
            sut.DocumentStoreConfiguration = _config;
            sut.Logger = new TestLogger(LoggerLevel.Debug);
            sut.ConfigService = new ConfigService();
            sut.Start(new List<string>() { TestConfig.ServerAddress.AbsoluteUri }, "TESTHANDLE");
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


    }
}
