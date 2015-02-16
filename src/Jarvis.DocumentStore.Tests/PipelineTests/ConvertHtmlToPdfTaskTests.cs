using Castle.Core.Logging;
using Jarvis.DocumentStore.Client;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Services;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.JobsHost.Processing.Conversions;
using Jarvis.DocumentStore.Tests.Support;
using Jarvis.Framework.Shared.IdentitySupport;
using Jarvis.Framework.Shared.MultitenantSupport;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.PipelineTests
{
    [TestFixture]
    public class ConvertHtmlToPdfTaskTests 
    {
        GridFsBlobStore _blobStore;
        BlobId _blobId;
        [SetUp]
        public void SetUp()
        {
            MongoDbTestConnectionProvider.DropTestsTenant();

            _blobStore = new GridFsBlobStore(MongoDbTestConnectionProvider.OriginalsDb, new InMemoryCounterService())
            {
                Logger = new ConsoleLogger()
            };

            var client = new DocumentStoreServiceClient(TestConfig.ServerAddress, TestConfig.Tenant);
            var zipped = client.ZipHtmlPage(TestConfig.PathToHtml);
            _blobId = _blobStore.Upload(Jarvis.DocumentStore.Core.Processing.DocumentFormats.ZHtml, zipped);
        }

        [Test]
        public void should_convert_htmlfolder_to_pdf()
        {
            var conversion = new HtmlToPdfConverter(_blobStore, new ConfigService())
            {
                Logger = new ConsoleLogger()
            };

            var newBlobId = conversion.Run(new TenantId("test"), _blobId);

            var fi = _blobStore.GetDescriptor(newBlobId);
            Assert.AreEqual("application/pdf", fi.ContentType);
        }
    }
}
