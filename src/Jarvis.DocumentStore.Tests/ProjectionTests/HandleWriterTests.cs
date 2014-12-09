using CQRS.Kernel.Commands;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Shared.MultitenantSupport;
using CQRS.Tests.DomainTests;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.ReadModel;
using Jarvis.DocumentStore.Host.Support;
using Jarvis.DocumentStore.Tests.PipelineTests;
using Jarvis.DocumentStore.Tests.Support;
using NUnit.Framework;

namespace Jarvis.DocumentStore.Tests.ProjectionTests
{
    [TestFixture]
    public class HandleWriterTests
    {
        private IHandleWriter _writer;
        private DocumentHandle _handle = new DocumentHandle("handle_1");
        private DocumentId _documentId = new DocumentId(1);
        private DocumentStoreBootstrapper _documentStoreService;

        [SetUp]
        public void SetUp()
        {
            var config = new DocumentStoreTestConfiguration();
            MongoDbTestConnectionProvider.DropAll();

            _documentStoreService = new DocumentStoreBootstrapper(TestConfig.ServerAddress);
            _documentStoreService.Start(config);

            TenantContext.Enter(new TenantId(TestConfig.Tenant));
            var tenant = ContainerAccessor.Instance.Resolve<TenantManager>().Current;
            _writer = tenant.Container.Resolve<IHandleWriter>();
        }

        [TearDown]
        public void TearDown()
        {
            _documentStoreService.Stop();
            BsonClassMapHelper.Clear();
        }


        [Test]
        public void with_promise_and_create_handle_should_be_linked_to_document_1()
        {
            _writer.Promise(_handle, 1);
            _writer.CreateIfMissing(_handle, 1);
            _writer.LinkDocument(_handle, _documentId, 2);

            var h = _writer.FindOneById(_handle);
            Assert.AreEqual(_documentId, h.DocumentId);
        }

        [Test]
        public void with_create_and_promise_handle_should_be_linked_to_document_1()
        {
            _writer.CreateIfMissing(_handle, 1);
            _writer.Promise(_handle, 1);
            _writer.LinkDocument(_handle, _documentId, 2);
        
            var h = _writer.FindOneById(_handle);
            Assert.AreEqual(_documentId, h.DocumentId);
        }

        [Test]
        public void with_link_and_promise_handle_should_be_linked_to_document_1()
        {
            _writer.CreateIfMissing(_handle, 1);
            _writer.LinkDocument(_handle, _documentId, 2);
            _writer.Promise(_handle, 1);
        
            var h = _writer.FindOneById(_handle);
            Assert.AreEqual(_documentId, h.DocumentId);
        }
    }
}