﻿using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Tests.PipelineTests;
using NUnit.Framework;

// ReSharper disable InconsistentNaming
namespace Jarvis.DocumentStore.Tests.SelfHostIntegratonTests
{
    [TestFixture, Explicit]
    public class upload_in_self_host_service
    {
        DocumentStoreBootstrapper _app;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _app = new DocumentStoreBootstrapper(TestConfig.ServerAddress);
            _app.Start();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            _app.Stop();
        }

        [Test]
        public void should_upload_all_documents()
        {
            var client = new DocumentStoreServiceClient(TestConfig.ServerAddress);

            Task.WaitAll(
                client.Upload(TestConfig.PathToWordDocument, "docx"),
                client.Upload(TestConfig.PathToExcelDocument, "xlsx"),
                client.Upload(TestConfig.PathToPowerpointDocument, "pptx"),
                client.Upload(TestConfig.PathToPowerpointShow, "ppsx"),
                client.Upload(TestConfig.PathToOpenDocumentText, "odt"),
                client.Upload(TestConfig.PathToOpenDocumentSpreadsheet, "ods"),
                client.Upload(TestConfig.PathToOpenDocumentPresentation, "odp"),
                client.Upload(TestConfig.PathToRTFDocument, "rtf"),
                client.Upload(TestConfig.PathToHtml, "html")
            );

            Debug.WriteLine("Done");
        }
    }
}