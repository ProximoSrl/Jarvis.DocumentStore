﻿using System;
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
using Jarvis.DocumentStore.Core.Storage;
using CQRS.Kernel.MultitenantSupport;
using System.Drawing;

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
        private IBlobStore _blobStore;

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
            TenantContext.Enter(new TenantId(TestConfig.Tenant));
            var tenant = ContainerAccessor.Instance.Resolve<TenantManager>().Current;
            _blobStore = tenant.Container.Resolve<IBlobStore>();
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
            sut.Logger = new TestLogger(LoggerLevel.Error);
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
            sut.Logger = new TestLogger(LoggerLevel.Error);
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

       
    }
}
