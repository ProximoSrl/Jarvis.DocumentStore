using Castle.Core.Logging;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Shared.IdentitySupport;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Tests.Support;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.PipelineTests
{
    [TestFixture]
    public class ConvertHtmlToPdfTaskTests : ITenantAccessor
    {
        GridFSFileStore _fileStore;

        [SetUp]
        public void SetUp()
        {
            MongoDbTestConnectionProvider.DropTenant1();
            Current = new Tenant(new DocumentStoreTest1Settings());

            _fileStore = new GridFSFileStore(this)
            {
                Logger = new ConsoleLogger()
            };

            var client = new DocumentStoreServiceClient(TestConfig.ServerAddress, TestConfig.Tenant);
            var zipped = client.ZipHtmlPage(TestConfig.PathToHtml);
            _fileStore.Upload(new FileId("ziphtml"), zipped);
        }

        [Test]
        public void should_convert_htmlfolder_to_pdf()
        {
            var conversion = new HtmlToPdfConverter(_fileStore, new ConfigService())
            {
                Logger = new ConsoleLogger()
            };

            var newFileId = conversion.Run(new TenantId("test"), new FileId("ziphtml"));

            var fi = _fileStore.GetDescriptor(newFileId);
            Assert.AreEqual("application/pdf", fi.ContentType);
        }

        public ITenant Current { get; private set; }
    }
}
